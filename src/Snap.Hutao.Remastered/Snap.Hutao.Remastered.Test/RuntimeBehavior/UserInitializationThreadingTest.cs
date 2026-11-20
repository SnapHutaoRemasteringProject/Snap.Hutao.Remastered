using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Concurrent;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class UserInitializationThreadingTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RefreshingAnAlreadyDisplayedAccountPublishesOnUiThread(bool startOnUi)
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync();
        Task<BindingUser> operation = startOnUi
            ? scenario.Ui.RunAsync(() => scenario.Service.ResumeUserAsync(user).AsTask())
            : Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask());

        await scenario.CompleteRequestsAsync(operation, userInfoValid: true, rolesValid: true);
        Assert.AreSame(user, await operation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreEqual("updated nickname", user.UserInfo?.Nickname);
        Assert.IsTrue(user.IsInitialized);
        CollectionAssert.AreEqual(new[] { "10001", "10002" }, scenario.Dependencies.SignedInUids);
        CollectionAssert.Contains(scenario.Notifications, nameof(BindingUser.UserInfo));
        CollectionAssert.Contains(scenario.Notifications, nameof(BindingUser.UserGameRoles));
    }

    [TestMethod]
    public async Task ARejectedOverseasUserInfoResponsePublishesItsFallbackOnUiThread()
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync(isOversea: true);
        Task<BindingUser> operation = Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask());
        await scenario.CompleteRequestsAsync(operation, userInfoValid: false, rolesValid: true);
        await operation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(SH.ModelBindingUserInitializationFailed, user.UserInfo?.Nickname);
        Assert.IsTrue(user.IsInitialized);
    }

    [TestMethod]
    public async Task AStoredChineseAccountExchangesBothLegacyCredentials()
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync(legacy: true);
        Task<BindingUser> operation = Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask());
        await scenario.CompleteRequestsAsync(operation, userInfoValid: true, rolesValid: true);
        await operation.WaitAsync(TimeSpan.FromSeconds(5));

        CollectionAssert.AreEqual(new[] { "LToken", "CookieToken" }, scenario.PassportClient.Requests);
        Assert.AreEqual("test-ltoken", user.LToken?[Cookie.LTOKEN_V2]);
        Assert.AreEqual("test-cookie-token", user.CookieToken?[Cookie.COOKIE_TOKEN_V2]);
        Assert.AreEqual("test-mid", user.LToken?[Cookie.LTMID_V2]);
        Assert.AreEqual("test-mid", user.CookieToken?[Cookie.ACCOUNT_MID_V2]);
        Assert.IsTrue(user.NeedDbUpdateAfterResume);
        Assert.IsTrue(user.IsInitialized);
    }

    [TestMethod]
    public async Task AFailedSecondExchangeDoesNotReplaceEitherStoredCredential()
    {
        using Scenario scenario = new();
        scenario.PassportClient.RejectCookieToken = true;
        BindingUser user = await scenario.CreateDisplayedUserAsync(legacy: true);
        Cookie? previousLToken = user.LToken;
        Cookie? previousCookieToken = user.CookieToken;

        await Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask()).WaitAsync(TimeSpan.FromSeconds(5));
        CollectionAssert.AreEqual(new[] { "LToken", "CookieToken" }, scenario.PassportClient.Requests);
        Assert.AreSame(previousLToken, user.LToken);
        Assert.AreSame(previousCookieToken, user.CookieToken);
        Assert.IsFalse(user.NeedDbUpdateAfterResume);
        Assert.IsFalse(user.IsInitialized);
        Assert.IsFalse(scenario.UserClient.Requested.Task.IsCompleted);
    }

    [TestMethod]
    public async Task ARejectedRoleResponseResetsTheDisplayedAccountOnUiThread()
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync();
        Task<BindingUser> operation = Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask());
        await scenario.CompleteRequestsAsync(operation, userInfoValid: true, rolesValid: false);
        await operation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(SH.ModelBindingUserInitializationFailed, user.UserInfo?.Nickname);
        Assert.IsFalse(user.IsInitialized);
        Assert.AreEqual(0, await scenario.Ui.RunAsync(() => Task.FromResult(user.UserGameRoles.Count())));
    }

    [TestMethod]
    public async Task MissingSTokenPublishesTheEarlyFailureOnUiThread()
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync();
        user.SToken = null;
        await Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask()).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(SH.ModelBindingUserInitializationFailed, user.UserInfo?.Nickname);
        Assert.IsFalse(user.IsInitialized);
        Assert.IsFalse(scenario.UserClient.Requested.Task.IsCompleted);
    }

    [TestMethod]
    public async Task CancellationBeforeALateResponseDoesNotPublishStaleUserInfo()
    {
        using Scenario scenario = new();
        using CancellationTokenSource cancellation = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync();
        UserInfo? originalInfo = user.UserInfo;
        Task<BindingUser> operation = Task.Run(() => scenario.Service.ResumeUserAsync(user, cancellation.Token).AsTask());
        await scenario.UserClient.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        scenario.UserClient.Completion.SetResult(new(new() { UserInfo = new() { Nickname = "late response" } }));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreSame(originalInfo, user.UserInfo);
        Assert.AreEqual(0, scenario.Notifications.Count);
    }

    private sealed class Scenario : IDisposable
    {
        private readonly ServiceProvider provider;

        public Scenario()
        {
            Dependencies = new(Ui);
            provider = new ServiceCollection()
                .AddSingleton<ITaskContext>(Ui)
                .AddSingleton<IUserFingerprintService>(Dependencies)
                .AddSingleton<IProfilePictureService>(Dependencies)
                .AddSingleton<IAutoSignInService>(Dependencies)
                .AddSingleton<IOverseaSupportFactory<IUserClient>>(new UserClientFactory(UserClient))
                .AddSingleton<IOverseaSupportFactory<IPassportClient>>(PassportClient)
                .AddSingleton(BindingClient)
                .BuildServiceProvider();
            Service = new(provider);
        }

        public UiThread Ui { get; } = new();
        public DelayedUserClient UserClient { get; } = new();
        public BindingClient BindingClient { get; } = new();
        public TestPassportClient PassportClient { get; } = new();
        public TestDependencies Dependencies { get; }
        public UserInitializationService Service { get; }
        public List<string> Notifications { get; } = [];

        public Task<BindingUser> CreateDisplayedUserAsync(bool isOversea = false, bool legacy = false) => Ui.RunAsync(() =>
        {
            BindingUser user = new() { UserInfo = new() { Nickname = "existing nickname" } };
            user.Entity.Aid = "101";
            user.Entity.Mid = "test-mid";
            user.Entity.IsOversea = isOversea;
            user.LToken = Cookie.FromLToken("101", "test-mid", "stored-ltoken", isOversea || legacy);
            user.CookieToken = Cookie.FromCookieToken("101", "test-mid", "stored-cookie-token", isOversea || legacy);
            user.PropertyChanged += (_, args) =>
            {
                // This is the thread-affine consumer corresponding to x:Bind's
                // immediate TextBlock/role-selector updates in UserView.
                Assert.AreEqual(Ui.ThreadId, Environment.CurrentManagedThreadId, $"Cross-thread publication: {args.PropertyName}");
                Notifications.Add(args.PropertyName ?? string.Empty);
            };
            return Task.FromResult(user);
        });

        public async Task CompleteRequestsAsync(Task operation, bool userInfoValid, bool rolesValid)
        {
            await UserClient.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            UserClient.Completion.SetResult(new(userInfoValid ? new() { UserInfo = new() { Nickname = "updated nickname" } } : null));
            await Task.WhenAny(BindingClient.Requested.Task, operation).WaitAsync(TimeSpan.FromSeconds(5));
            if (!BindingClient.Requested.Task.IsCompleted) await operation;
            await BindingClient.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            BindingClient.Completion.SetResult(new(rolesValid ? new() { List = [new("10001"), new("10002")] } : null));
        }

        public void Dispose()
        {
            provider.Dispose();
            Ui.Dispose();
        }
    }

    private sealed class DelayedUserClient : IUserClient
    {
        public TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Response<UserFullInfoWrapper>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<Response<UserFullInfoWrapper>> GetUserFullInfoAsync(Model.Entity.User user, CancellationToken token)
        {
            Requested.TrySetResult();
            return new(Completion.Task);
        }
    }

    private sealed class UserClientFactory(IUserClient client) : IOverseaSupportFactory<IUserClient>
    {
        public IUserClient Create(bool isOversea) => client;
    }

    private sealed class TestPassportClient : IPassportClient, IOverseaSupportFactory<IPassportClient>
    {
        public List<string> Requests { get; } = [];
        public bool RejectCookieToken { get; set; }

        public IPassportClient Create(bool isOversea) => this;

        public ValueTask<Response<LTokenWrapper>> GetLTokenBySTokenAsync(Model.Entity.User user, CancellationToken token)
        {
            Requests.Add("LToken");
            return new(new Response<LTokenWrapper>(new()));
        }

        public ValueTask<Response<UidCookieToken>> GetCookieAccountInfoBySTokenAsync(Model.Entity.User user, CancellationToken token)
        {
            Requests.Add("CookieToken");
            return new(new Response<UidCookieToken>(RejectCookieToken ? null : new()));
        }
    }

    private sealed class TestDependencies(UiThread ui) : IUserFingerprintService, IProfilePictureService, IAutoSignInService
    {
        public List<string> SignedInUids { get; } = [];

        public async ValueTask TryInitializeAsync(BindingUser user, CancellationToken token)
        {
            // Downstream work can finish on a worker even if a previous helper
            // switched to UI. The service must switch again before publishing.
            await ui.SwitchToBackgroundAsync();
        }

        public ValueTask RefreshUserGameRoleAsync(UserGameRole userGameRole, CancellationToken token) => ValueTask.CompletedTask;

        public async ValueTask InitializeAsync(UserAndUid userAndUid, CancellationToken token)
        {
            Assert.AreNotEqual(ui.ThreadId, Environment.CurrentManagedThreadId);
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            SignedInUids.Add(userAndUid.Uid);
        }
    }

    private sealed class UiThread : ITaskContext, IDisposable
    {
        private readonly BlockingCollection<Action> queue = new();
        private readonly Thread thread;

        public UiThread()
        {
            using ManualResetEventSlim ready = new();
            thread = new(() =>
            {
                ThreadId = Environment.CurrentManagedThreadId;
                SynchronizationContext.SetSynchronizationContext(new QueueSynchronizationContext(this));
                ready.Set();
                foreach (Action action in queue.GetConsumingEnumerable()) action();
            }) { IsBackground = true };
            thread.Start();
            Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(5)));
        }

        public int ThreadId { get; private set; }
        public UserTestThreadSwitch SwitchToMainThreadAsync() => new(() => Environment.CurrentManagedThreadId == ThreadId, queue.Add);
        public UserTestThreadSwitch SwitchToBackgroundAsync() => new(() => Environment.CurrentManagedThreadId != ThreadId, continuation => ThreadPool.QueueUserWorkItem(_ => continuation()));

        public Task<T> RunAsync<T>(Func<Task<T>> operation)
        {
            TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            queue.Add(async () =>
            {
                try { completion.SetResult(await operation()); }
                catch (Exception exception) { completion.SetException(exception); }
            });
            return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            queue.CompleteAdding();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(5)));
            queue.Dispose();
        }

        private sealed class QueueSynchronizationContext(UiThread owner) : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object? state) => owner.queue.Add(() => callback(state));
        }
    }
}
