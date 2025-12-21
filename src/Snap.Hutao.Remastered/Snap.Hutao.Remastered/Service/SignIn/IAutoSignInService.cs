namespace Snap.Hutao.Remastered.Service.SignIn;

internal interface IAutoSignInService
{
    ValueTask InitializeAsync(CancellationToken token = default);

    ValueTask RunOnceAsync(CancellationToken token = default);

    bool IsEnabled { get; set; }
}
