using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Concurrent;
using System.ComponentModel;
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
        CollectionAssert.Contains(scenario.Notifications, nameof(BindingUser.IsInitialized));
    }

    [TestMethod]
    public async Task ARejectedUserInfoResponsePublishesItsFallbackOnUiThread()
    {
        using Scenario scenario = new();
        BindingUser user = await scenario.CreateDisplayedUserAsync();
        Task<BindingUser> operation = Task.Run(() => scenario.Service.ResumeUserAsync(user).AsTask());
        await scenario.CompleteRequestsAsync(operation, userInfoValid: false, rolesValid: true);
        await operation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(SH.ModelBindingUserInitializationFailed, user.UserInfo?.Nickname);
        Assert.IsTrue(user.IsInitialized);
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
                .AddSingleton(BindingClient)
                .BuildServiceProvider();
            Service = new(provider);
        }

        public UiThread Ui { get; } = new();
        public DelayedUserClient UserClient { get; } = new();
        public BindingClient BindingClient { get; } = new();
        public TestDependencies Dependencies { get; }
        public UserInitializationService Service { get; }
        public List<string> Notifications { get; } = [];

        public Task<BindingUser> CreateDisplayedUserAsync() => Ui.RunAsync(() =>
        {
            BindingUser user = new() { UserInfo = new() { Nickname = "existing nickname" } };
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
