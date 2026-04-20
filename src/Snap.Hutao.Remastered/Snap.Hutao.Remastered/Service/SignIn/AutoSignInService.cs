using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.ViewModel.User;

namespace Snap.Hutao.Remastered.Service.SignIn;

[Service(ServiceLifetime.Singleton, typeof(IAutoSignInService))]
public sealed partial class AutoSignInService : IAutoSignInService
{
    private readonly ISignInService signInService;

    [GeneratedConstructor]
    public partial AutoSignInService(IServiceProvider serviceProvider);

    public bool IsEnabled
    {
        get => LocalSetting.Get(SettingKeys.AutoSignInEnabled, true);
        set => LocalSetting.Set(SettingKeys.AutoSignInEnabled, value);
    }

    public async ValueTask InitializeAsync(UserAndUid userAndUid, CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await RunOnceAsync(userAndUid, token).ConfigureAwait(false);
    }

    public async ValueTask RunOnceAsync(UserAndUid userAndUid, CancellationToken token = default)
    {
        if (!IsEnabled)
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
