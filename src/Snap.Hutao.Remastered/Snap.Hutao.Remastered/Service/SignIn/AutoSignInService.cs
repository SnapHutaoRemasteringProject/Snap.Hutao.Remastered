using Sentry;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.ViewModel.User;

namespace Snap.Hutao.Remastered.Service.SignIn;

[Service(ServiceLifetime.Singleton, typeof(IAutoSignInService))]
internal sealed partial class AutoSignInService : IAutoSignInService
{
    private readonly IUserService userService;
    private readonly ISignInService signInService;

    [GeneratedConstructor]
    public partial AutoSignInService(IServiceProvider serviceProvider);

    public bool IsEnabled
    {
        get => LocalSetting.Get(SettingKeys.AutoSignInEnabled, false);
        set => LocalSetting.Set(SettingKeys.AutoSignInEnabled, value);
    }

    public async ValueTask InitializeAsync(CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await RunOnceAsync(token).ConfigureAwait(false);
    }

    public async ValueTask RunOnceAsync(CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        // Try to sign in for current selected user and uid
        if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { } userAndUid)
        {
            return;
        }

        try
        {
            await signInService.ClaimSignInRewardAsync(userAndUid, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
