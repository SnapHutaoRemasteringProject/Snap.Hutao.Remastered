// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.ViewModel.TravelersDiary;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class TravelersDiaryViewModel : Abstraction.ViewModelSlim
{
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;

    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial TravelersDiaryViewModel(IServiceProvider serviceProvider);

    // This property must be a reference type
    [ObservableProperty]
    public partial Ledger? Ledger { get; set; }

    /// <inheritdoc/>
    protected override async Task LoadAsync()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { } userAndUid)
            {
                await taskContext.SwitchToMainThreadAsync();
                IsInitialized = true;
                return;
            }

            try
            {
                IGameRecordClient gameRecordClient = scope.ServiceProvider
                    .GetRequiredService<IOverseaSupportFactory<IGameRecordClient>>()
                    .CreateFor(userAndUid);

                Response<Ledger> response = await gameRecordClient.GetLedgerAsync(userAndUid, 0, CancellationToken.None).ConfigureAwait(false);
                if (ResponseValidator.TryValidateWithoutUINotification(response, out Ledger? ledger))
                {
                    await taskContext.SwitchToMainThreadAsync();
                    Ledger = ledger;
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                messenger.Send(InfoBarMessage.Error(ex));
            }
        }
    }
}
