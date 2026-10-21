// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.ViewModel.User;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

[Service(ServiceLifetime.Singleton)]
public sealed class AccountInsightsWindows : IRecipient<UserAndUidChangedMessage>
{
    private readonly IServiceProvider serviceProvider;
    private AccountInsightsWindow? current;
    private string? currentUid;

    public AccountInsightsWindows(IServiceProvider serviceProvider, IMessenger messenger)
    {
        this.serviceProvider = serviceProvider;
        messenger.Register(this);
    }

    public async Task OpenAsync()
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var selected = await scope.ServiceProvider.GetRequiredService<IUserService>().GetCurrentUserAndUidAsync();
        string? uid = selected?.Uid.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (current is not null && currentUid != uid) { CloseCurrent(); }
        if (current is null)
        {
            AccountInsightsWindow window = serviceProvider.GetRequiredService<AccountInsightsWindow>();
            current = window;
            currentUid = uid;
            window.Closed += (_, args) =>
            {
                if (!args.Handled && ReferenceEquals(current, window))
                {
                    current = null;
                    currentUid = null;
                }
            };
        }
        current.Activate();
    }

    public void Receive(UserAndUidChangedMessage message)
    {
        string? uid = message.UserAndUid?.Uid.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (current is not null && currentUid != uid) { CloseCurrent(); }
    }

    private void CloseCurrent()
    {
        AccountInsightsWindow? window = current;
        current = null;
        currentUid = null;
        window?.Close();
    }
}
