// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Game.Package;
using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

namespace Snap.Hutao.Remastered.ViewModel.Game;

[Service(ServiceLifetime.Transient)]
internal sealed partial class EmbeddedYaeLaunchExecutionViewModel : IViewModelSupportLaunchExecution
{
    private readonly IServiceProvider serviceProvider;
    private readonly LaunchOptions launchOptions;
    private readonly IGameService gameService;
    private readonly ITaskContext taskContext;
    private readonly LaunchGameShared shared;
    private readonly IMessenger messenger;

    [GeneratedConstructor]
    public partial EmbeddedYaeLaunchExecutionViewModel(IServiceProvider serviceProvider);

    public LaunchSchemeFilteredGameAccountsView CurrentSchemeFilteredGameAccountsView { get => field ??= new(Property.Create(false), gameService, taskContext, messenger); }

    LaunchScheme? IViewModelSupportLaunchExecution.TargetScheme { get => CurrentSchemeFilteredGameAccountsView.Scheme; }

    LaunchScheme? IViewModelSupportLaunchExecution.CurrentScheme { get => CurrentSchemeFilteredGameAccountsView.Scheme; }

    GameAccount? IViewModelSupportLaunchExecution.GameAccount { get => CurrentSchemeFilteredGameAccountsView.View?.CurrentItem; }

    ValueTask<BlockDeferral<PackageConvertStatus>> IViewModelSupportLaunchExecution.CreateConvertBlockDeferralAsync()
    {
        return BlockDeferral<PackageConvertStatus>.CreateAsync<LaunchGamePackageConvertDialog>(serviceProvider, static (state, dialog) => dialog.State = state);
    }

    public async ValueTask<bool> InitializeAsync()
    {
        try
        {
            LaunchScheme? currentScheme = launchOptions.GamePathEntry.Value is not null
                ? shared.GetCurrentLaunchSchemeFromConfigurationFile()
                : default;

            await taskContext.SwitchToMainThreadAsync();
            await CurrentSchemeFilteredGameAccountsView.SetAsync(currentScheme).ConfigureAwait(true);
            return true;
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(ex));
            return false;
        }
    }
}