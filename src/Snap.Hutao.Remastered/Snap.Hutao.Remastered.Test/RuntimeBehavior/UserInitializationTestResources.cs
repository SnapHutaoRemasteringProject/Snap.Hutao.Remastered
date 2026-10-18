// The portable test assembly executes the real initialization service. Only
// Windows-bound models, HTTP endpoints and the generated constructor are supplied
// here; no initialization, response ordering or UI publication logic is copied.
global using Snap.Hutao.Remastered.Core.Threading;
global using Snap.Hutao.Remastered.Service.Job;
global using Snap.Hutao.Remastered.Service.SignIn;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction
{
    public interface IOverseaSupportFactory<out T>
    {
        T Create(bool isOversea);
    }
}

namespace Snap.Hutao.Remastered.Core.Threading
{
    public interface ITaskContext
    {
        UserTestThreadSwitch SwitchToMainThreadAsync();
        UserTestThreadSwitch SwitchToBackgroundAsync();
    }

    public readonly struct UserTestThreadSwitch(Func<bool> hasAccess, Action<Action> dispatch) : INotifyCompletion
    {
        public bool IsCompleted => hasAccess();
        public UserTestThreadSwitch GetAwaiter() => this;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => dispatch(continuation);
    }
}

namespace Snap.Hutao.Remastered.UI.Xaml.Data
{
    public sealed class AdvancedCollectionView<T>(IList<T> source) : IEnumerable<T>
    {
        private readonly int ownerThread = Environment.CurrentManagedThreadId;

        public IEnumerator<T> GetEnumerator()
        {
            // A role view is bound to UI consumers; sign-in must enumerate a
            // snapshot rather than resume its enumerator after a network await.
            Assert.AreEqual(ownerThread, Environment.CurrentManagedThreadId, "Role view enumerated outside its owning thread.");
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class UserTestCollectionExtension
    {
        public static AdvancedCollectionView<T> AsAdvancedCollectionView<T>(this IList<T> source) => new(source);
    }
}

namespace Snap.Hutao.Remastered.Web.Hoyolab
{
    public sealed class Cookie : Dictionary<string, string>
    {
        public const string STUID = "stuid";
        public const string MID = "mid";
        public const string LTUID = "ltuid";
        public const string LTOKEN = "ltoken";
        public const string ACCOUNT_ID = "account_id";
        public const string COOKIE_TOKEN = "cookie_token";
    }

    public sealed class ListWrapper<T>
    {
        public List<T> List { get; set; } = [];
    }
}

namespace Snap.Hutao.Remastered.Model.Entity
{
    public sealed class User
    {
        public string? Aid { get; set; }
        public string? Mid { get; set; }
        public bool IsOversea { get; set; }
        public string? Fingerprint { get; set; }
        public DateTimeOffset FingerprintLastUpdateTime { get; set; }
        public DateTimeOffset CookieTokenLastUpdateTime { get; set; } = DateTimeOffset.UtcNow;
        public static User From(Web.Hoyolab.Cookie cookie) => new();
    }
}

namespace Snap.Hutao.Remastered.ViewModel.User
{
    public sealed partial class User : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsInitialized { get; set; }

        [ObservableProperty]
        public partial Web.Hoyolab.Bbs.User.UserInfo? UserInfo { get; set; }

        [ObservableProperty]
        public partial UI.Xaml.Data.AdvancedCollectionView<Web.Hoyolab.Takumi.Binding.UserGameRole> UserGameRoles { get; set; } = new([]);

        public Model.Entity.User Entity { get; } = new();
        public bool IsOversea => Entity.IsOversea;
        public bool NeedDbUpdateAfterResume { get; set; }
        public Web.Hoyolab.Cookie? SToken { get; set; } = new();
        public Web.Hoyolab.Cookie? LToken { get; set; } = new();
        public Web.Hoyolab.Cookie? CookieToken { get; set; } = new();
        public static User From(Model.Entity.User entity, IServiceProvider provider) => new();
    }

    public sealed record UserAndUid(Model.Entity.User User, string Uid)
    {
        public static UserAndUid From(Model.Entity.User user, string uid) => new(user, uid);
    }
}

namespace Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User
{
    public sealed class UserInfo
    {
        public string? Uid { get; set; }
        public string? Nickname { get; set; }
    }

    public sealed class UserFullInfoWrapper
    {
        public UserInfo UserInfo { get; set; } = new();
    }

    public interface IUserClient
    {
        ValueTask<Response.Response<UserFullInfoWrapper>> GetUserFullInfoAsync(Model.Entity.User user, CancellationToken token);
    }
}

namespace Snap.Hutao.Remastered.Web.Hoyolab.Passport
{
    public sealed class LTokenWrapper
    {
        public string LToken { get; set; } = "test-ltoken";
    }

    public sealed class UidCookieToken
    {
        public string CookieToken { get; set; } = "test-cookie-token";
    }

    public interface IPassportClient
    {
        ValueTask<Response.Response<LTokenWrapper>> GetLTokenBySTokenAsync(Model.Entity.User user, CancellationToken token);
        ValueTask<Response.Response<UidCookieToken>> GetCookieAccountInfoBySTokenAsync(Model.Entity.User user, CancellationToken token);
    }
}

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding
{
    public sealed record UserGameRole(string GameUid);

    public sealed class BindingClient
    {
        public TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Response.Response<ListWrapper<UserGameRole>>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<Response.Response<ListWrapper<UserGameRole>>> GetUserGameRolesOverseaAwareAsync(Model.Entity.User user, CancellationToken token)
        {
            Requested.TrySetResult();
            return new(Completion.Task);
        }
    }
}

namespace Snap.Hutao.Remastered.Web.Response
{
    public sealed record Response<T>(T? Data) where T : class;

    public static class ResponseValidator
    {
        public static bool TryValidate<T>(Response<T> response, IServiceProvider provider, [NotNullWhen(true)] out T? data) where T : class
        {
            data = response.Data;
            return data is not null;
        }
    }
}

namespace Snap.Hutao.Remastered.Service.SignIn
{
    public interface IAutoSignInService
    {
        ValueTask InitializeAsync(ViewModel.User.UserAndUid userAndUid, CancellationToken token);
    }
}

namespace Snap.Hutao.Remastered.Service.User
{
    public sealed record InputCookie(Web.Hoyolab.Cookie Cookie, bool IsOversea, string? DeviceFp);

    public static class SH
    {
        public const string ModelBindingUserInitializationFailed = "Initialization failed";
    }

    public sealed partial class UserInitializationService
    {
        public partial UserInitializationService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            taskContext = serviceProvider.GetRequiredService<ITaskContext>();
            userFingerprintService = serviceProvider.GetRequiredService<IUserFingerprintService>();
            profilePictureService = serviceProvider.GetRequiredService<IProfilePictureService>();
            autoSignInService = serviceProvider.GetRequiredService<IAutoSignInService>();
        }
    }
}
