// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Backpack;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.Service.Cultivation.Consumption;
using Snap.Hutao.Remastered.Service.Cultivation.Offline;
using Snap.Hutao.Remastered.Service.Inventory;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Yae;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.ViewModel.Game;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using AvatarPromotionDelta = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate.AvatarPromotionDelta;
using CalculateBatchConsumption = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate.BatchConsumption;
using PromotionDelta = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate.PromotionDelta;

namespace Snap.Hutao.Remastered.ViewModel.Cultivation;

[SuppressMessage("", "CA1001")]
[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class CultivationViewModel : Abstraction.ViewModel
{
    private readonly ExclusiveTokenProvider exclusiveTokenProvider = new();

    private readonly IBackpackService backpackService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ICultivationService cultivationService;
    private readonly INavigationService navigationService;
    private readonly IInventoryService inventoryService;
    private readonly IServiceProvider serviceProvider;
    private readonly IMetadataService metadataService;
    private readonly ITaskContext taskContext;
    private readonly IYaeService yaeService;
    private readonly IMessenger messenger;

    private CultivationMetadataContext? metadataContext;

    [GeneratedConstructor]
    public partial CultivationViewModel(IServiceProvider serviceProvider);

    public IAdvancedDbCollectionView<CultivateProject>? Projects
    {
        get;
        set
        {
            AdvancedCollectionViewCurrentChanged.Detach(field, OnCurrentProjectChanged);
            SetProperty(ref field, value);
            AdvancedCollectionViewCurrentChanged.Attach(field, OnCurrentProjectChanged);
        }
    }

    [ObservableProperty]
    public partial ImmutableArray<InventoryItemView> InventoryItems { get; set; } = [];

    [ObservableProperty]
    public partial IAdvancedCollectionView<CultivateEntryView>? CultivateEntries { get; set; }

    [ObservableProperty]
    public partial bool EntriesUpdating { get; set; }

    [ObservableProperty]
    public partial bool IncompleteFirst { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<StatisticsCultivateItem>? StatisticsItems { get; set; }

    [ObservableProperty]
    public partial ResinStatistics? ResinStatistics { get; set; }

    [ObservableProperty]
    public partial SearchData? SearchData { get; set; }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        if (!await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            return false;
        }

        metadataContext = await metadataService.GetContextAsync<CultivationMetadataContext>(token).ConfigureAwait(false);
        SearchData searchData = SearchData.CreateForCultivation();

        using (await EnterCriticalSectionAsync().ConfigureAwait(false))
        {
            IAdvancedDbCollectionView<CultivateProject> projects = await cultivationService.GetProjectCollectionAsync().ConfigureAwait(false);
            await taskContext.SwitchToMainThreadAsync();
            Projects = projects;
            Projects.MoveCurrentTo(Projects.Source.SelectedOrFirstOrDefault());
        }

        // Force update when re-entering the page
        if (Projects.CurrentItem is not null && CultivateEntries is null)
        {
            await UpdateEntryCollectionAsync(Projects.CurrentItem).ConfigureAwait(false);
        }

        await taskContext.SwitchToMainThreadAsync();
        SearchData = searchData;

        return true;
    }

    protected override void UninitializeOverride()
    {
        using (Projects?.SuppressChangeCurrentItem())
        {
            Projects = default;
        }
    }

    private void OnCurrentProjectChanged(object? sender, object? e)
    {
        UpdateEntryCollectionAsync(Projects?.CurrentItem).SafeForget();
    }

    [Command("AddProjectCommand")]
    private async Task AddProjectAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Add project", "CultivationViewModel.Command"));

        CultivateProjectDialog dialog = await contentDialogFactory.CreateInstanceAsync<CultivateProjectDialog>(serviceProvider).ConfigureAwait(false);
        (bool isOk, CultivateProject project) = await dialog.CreateProjectAsync().ConfigureAwait(false);

        if (!isOk)
        {
            return;
        }

        InfoBarMessage message = await cultivationService.TryAddProjectAsync(project).ConfigureAwait(false) switch
        {
            ProjectAddResultKind.Added => InfoBarMessage.Success(SH.ViewModelCultivationProjectAdded),
            ProjectAddResultKind.InvalidName => InfoBarMessage.Information(SH.ViewModelCultivationProjectInvalidName),
            ProjectAddResultKind.AlreadyExists => InfoBarMessage.Information(SH.ViewModelCultivationProjectAlreadyExists),
            _ => throw HutaoException.NotSupported(),
        };

        messenger.Send(message);
    }

    [Command("RemoveProjectCommand")]
    private async Task RemoveProjectAsync(CultivateProject? project)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove project", "CultivationViewModel.Command"));

        if (project is null)
        {
            return;
        }

        ContentDialogResult result = await contentDialogFactory
            .CreateForConfirmCancelAsync(
                SH.ViewModelCultivationRemoveProjectTitle,
                SH.ViewModelCultivationRemoveProjectContent)
            .ConfigureAwait(false);

        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        await cultivationService.RemoveProjectAsync(project).ConfigureAwait(false);
        await taskContext.SwitchToMainThreadAsync();
        Projects?.MoveCurrentToFirst();
    }

    private async ValueTask UpdateEntryCollectionAsync(CultivateProject? project)
    {
        if (project is null)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        EntriesUpdating = true;

        try
        {
            CultivationMetadataContext context = await metadataService
                .GetContextAsync<CultivationMetadataContext>()
                .ConfigureAwait(false);

            ObservableCollection<CultivateEntryView> entries = await cultivationService
                .GetCultivateEntryCollectionAsync(project, context)
                .ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();

            IAdvancedCollectionView<CultivateEntryView> entriesView = entries.AsAdvancedCollectionView();
            CultivateEntries = entriesView;

            await UpdateInventoryItemsAsync().ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();

            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
        finally
        {
            await taskContext.SwitchToMainThreadAsync();
            EntriesUpdating = false;
        }
    }

    [Command("RemoveEntryCommand")]
    private async Task RemoveEntryAsync(CultivateEntryView? entry)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove entry", "CultivationViewModel.Command"));

        if (entry is not null)
        {
            ArgumentNullException.ThrowIfNull(CultivateEntries);
            CultivateEntries.Remove(entry);
            await cultivationService.RemoveCultivateEntryAsync(entry.EntryId).ConfigureAwait(false);
            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
    }

    [Command("FinishStateCommand")]
    private async Task UpdateFinishedStateAsync(CultivateItemView? item)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Toggle item finish state", "CultivationViewModel.Command"));

        if (item is not null)
        {
            item.IsFinished = !item.IsFinished;
            cultivationService.SaveCultivateItem(item);
            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
    }

    [Command("SaveInventoryItemCommand")]
    private async Task SaveInventoryItemAsync(InventoryItemView? inventoryItem)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Save inventory item", "CultivationViewModel.Command"));

        if (inventoryItem is not null)
        {
            inventoryService.SaveInventoryItem(inventoryItem);
            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
    }

    [Command("RefreshInventoryByEmbeddedYaeCommand")]
    private async Task RefreshInventoryByEmbeddedYaeAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Refresh inventory", "CultivationViewModel.Command", [("source", "Embedded Yae")]));

        if (Projects?.CurrentItem is null || metadataContext is null)
        {
            return;
        }

        if (!HutaoRuntime.IsProcessElevated)
        {
            await contentDialogFactory.CreateForConfirmAsync(SH.ViewModelYaeProcessNotElevatedTitle, SH.ViewModelYaeProcessNotElevatedDescription).ConfigureAwait(false);
            return;
        }

        using (await EnterCriticalSectionAsync().ConfigureAwait(false))
        {
            EmbeddedYaeLaunchExecutionViewModel viewModel = serviceProvider.GetRequiredService<EmbeddedYaeLaunchExecutionViewModel>();
            if (!await viewModel.InitializeAsync().ConfigureAwait(false))
            {
                return;
            }

            RefreshOptions options = RefreshOptions.CreateForEmbeddedYae(Projects.CurrentItem, yaeService, viewModel);
            await inventoryService.RefreshInventoryAsync(options).ConfigureAwait(false);

            await UpdateInventoryItemsAsync().ConfigureAwait(false);
            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
    }

    [Command("RefreshInventoryByCalculatorCommand")]
    private async Task RefreshInventoryByCalculatorAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Refresh inventory", "CultivationViewModel.Command", [("source", "Web Calculator")]));

        if (Projects?.CurrentItem is null || metadataContext is null)
        {
            return;
        }

        using (await EnterCriticalSectionAsync().ConfigureAwait(false))
        {
            ContentDialog dialog = await contentDialogFactory
                .CreateForIndeterminateProgressAsync(SH.ViewModelCultivationRefreshInventoryProgress)
                .ConfigureAwait(false);

            using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
            {
                await inventoryService.RefreshInventoryAsync(RefreshOptions.CreateForWebCalculator(Projects.CurrentItem, metadataContext)).ConfigureAwait(false);

                await UpdateInventoryItemsAsync().ConfigureAwait(false);
                await UpdateStatisticsItemsAsync().ConfigureAwait(false);
            }
        }
    }

    [Command("RefreshInventoryByBackpackArchiveCommand")]
    private async Task RefreshInventoryByBackpackArchiveAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Refresh inventory", "CultivationViewModel.Command", [("source", "Backpack Archive")]));

        if (Projects?.CurrentItem is null || metadataContext is null)
        {
            return;
        }

        BackpackArchivePickerDialog dialog = await contentDialogFactory.CreateInstanceAsync<BackpackArchivePickerDialog>(serviceProvider).ConfigureAwait(false);
        (bool isOk, BackpackArchive archive) = await dialog.GetSelectedArchiveAsync().ConfigureAwait(false);

        if (!isOk || archive is null)
        {
            return;
        }

        using (await EnterCriticalSectionAsync().ConfigureAwait(false))
        {
            ContentDialog waitDialog = await contentDialogFactory
                .CreateForIndeterminateProgressAsync(SH.ViewModelCultivationRefreshInventoryProgress)
                .ConfigureAwait(false);

            using (await contentDialogFactory.BlockAsync(waitDialog).ConfigureAwait(false))
            {
                ImmutableArray<BackpackItem> backpackItems = backpackService.GetBackpackItemImmutableArrayByArchiveId(archive.InnerId);
                inventoryService.SaveInventoryItemsFromBackpackArchive(Projects.CurrentItem, backpackItems, metadataContext);

                await UpdateInventoryItemsAsync().ConfigureAwait(false);
                await UpdateStatisticsItemsAsync().ConfigureAwait(false);
            }
        }
    }

    [Command("ClearInventoryCommand")]
    private async Task ClearInventoryAsync(CultivateProject? project)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Clear inventory", "CultivationViewModel.Command"));

        if (project is null)
        {
            return;
        }

        ContentDialogResult result = await contentDialogFactory
            .CreateForConfirmCancelAsync(
                SH.ViewModelCultivationClearInventoryTitle,
                SH.ViewModelCultivationClearInventoryContent)
            .ConfigureAwait(false);

        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        ContentDialog dialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ViewModelCultivationClearInventoryProgress)
            .ConfigureAwait(false);
        using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
        {
            inventoryService.RemoveInventoryItems(project);

            await UpdateInventoryItemsAsync().ConfigureAwait(false);
            await UpdateStatisticsItemsAsync().ConfigureAwait(false);
        }
    }

    [Command("RefreshStatisticsItemsCommand")]
    private async Task UpdateStatisticsItemsAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Refresh statistics items", "CultivationViewModel.Command"));

        // https://github.com/DGP-Studio/Snap.Hutao.Remastered/issues/2044
        // Force clear the list and bring view to the top to prevent UI dead loop
        await taskContext.SwitchToMainThreadAsync();
        StatisticsItems = null;
        ResinStatistics = null;

        if (Projects?.CurrentItem is null)
        {
            return;
        }

        if (metadataContext is null)
        {
            return;
        }

        await taskContext.SwitchToBackgroundAsync();

        CancellationToken token = exclusiveTokenProvider.GetNewToken();
        StatisticsCultivateItemCollection statistics;
        ResinStatistics resinStatistics;
        try
        {
            statistics = await cultivationService.GetStatisticsCultivateItemCollectionAsync(Projects.CurrentItem, metadataContext, token).ConfigureAwait(false);
            resinStatistics = await cultivationService.GetResinStatisticsAsync(statistics, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (IncompleteFirst)
        {
            statistics.SortAsIncompleteFirst();
        }

        await taskContext.SwitchToMainThreadAsync();
        StatisticsItems = statistics.ToObservableCollection();
        ResinStatistics = resinStatistics;
    }

    private async ValueTask UpdateInventoryItemsAsync()
    {
        // https://github.com/DGP-Studio/Snap.Hutao.Remastered/issues/2044
        // Force clear the list and bring view to the top to prevent UI dead loop
        await taskContext.SwitchToMainThreadAsync();
        InventoryItems = [];

        if (Projects?.CurrentItem is null || metadataContext is null)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        InventoryItems = inventoryService.GetInventoryItemViews(metadataContext, Projects.CurrentItem, SaveInventoryItemCommand);
    }

    [Command("ModifyEntryCommand")]
    private async Task ModifyEntryAsync(CultivateEntryView? entryView)
    {
        await taskContext.SwitchToMainThreadAsync();

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Modify entry", "CultivationViewModel.Command"));

        if (entryView?.Entry is not { } entry)
        {
            return;
        }

        if (metadataContext is null || Projects?.CurrentItem is null)
        {
            return;
        }

        AvatarPromotionDelta delta = new();

        switch (entry.Type)
        {
            case Model.Entity.Primitive.CultivateType.AvatarAndSkill:
                {
                    if (entryView.Avatar is not { } calculableAvatar)
                    {
                        return;
                    }

                    delta.AvatarId = calculableAvatar.AvatarId;
                    delta.AvatarLevelCurrent = Math.Clamp(calculableAvatar.LevelCurrent, calculableAvatar.LevelMin, calculableAvatar.LevelMax);
                    delta.AvatarLevelTarget = Math.Clamp(calculableAvatar.LevelTarget, calculableAvatar.LevelMin, calculableAvatar.LevelMax);
                    delta.AvatarPromoteLevel = calculableAvatar.PromoteLevel;
                    delta.SkillList = calculableAvatar.Skills.SelectAsArray(static skill => new PromotionDelta
                    {
                        Id = skill.GroupId,
                        LevelCurrent = Math.Clamp(skill.LevelCurrent, skill.LevelMin, skill.LevelMax),
                        LevelTarget = Math.Clamp(skill.LevelTarget, skill.LevelMin, skill.LevelMax),
                    });

                    break;
                }

            case Model.Entity.Primitive.CultivateType.Weapon:
                {
                    if (entryView.Weapon is not { } calculableWeapon)
                    {
                        return;
                    }

                    delta.Weapon = new PromotionDelta
                    {
                        Id = calculableWeapon.WeaponId,
                        LevelCurrent = Math.Clamp(calculableWeapon.LevelCurrent, calculableWeapon.LevelMin, calculableWeapon.LevelMax),
                        LevelTarget = Math.Clamp(calculableWeapon.LevelTarget, calculableWeapon.LevelMin, calculableWeapon.LevelMax),
                        WeaponPromoteLevel = calculableWeapon.PromoteLevel,
                    };

                    break;
                }

            default:
                return;
        }

        CalculateBatchConsumption batchConsumption;
        InputConsumption input;

        switch (entry.Type)
        {
            case Model.Entity.Primitive.CultivateType.AvatarAndSkill:
                {
                    Model.Metadata.Avatar.Avatar avatar = metadataContext.IdAvatarMap[entry.Id];

                    batchConsumption = OfflineCalculator.CalculateWikiAvatarConsumption(delta, avatar);

                    if (batchConsumption.OverallConsume.IsEmpty)
                    {
                        messenger.Send(InfoBarMessage.Warning(SH.ViewModelCultivationEntryAddNoConsumptionWarning));
                        return;
                    }

                    input = new()
                    {
                        Type = Model.Entity.Primitive.CultivateType.AvatarAndSkill,
                        ItemId = avatar.Id,
                        Items = batchConsumption.OverallConsume,
                        LevelInformation = LevelInformation.From(delta),
                        Strategy = ConsumptionSaveStrategyKind.OverwriteExisting,
                    };

                    break;
                }

            case Model.Entity.Primitive.CultivateType.Weapon:
                {
                    Model.Metadata.Weapon.Weapon weapon = metadataContext.IdWeaponMap[entry.Id];

                    batchConsumption = OfflineCalculator.CalculateWikiWeaponConsumption(delta, weapon);

                    if (batchConsumption.OverallConsume.IsEmpty)
                    {
                        messenger.Send(InfoBarMessage.Warning(SH.ViewModelCultivationEntryAddNoConsumptionWarning));
                        return;
                    }

                    input = new()
                    {
                        Type = Model.Entity.Primitive.CultivateType.Weapon,
                        ItemId = weapon.Id,
                        Items = batchConsumption.OverallConsume,
                        LevelInformation = LevelInformation.From(delta),
                        Strategy = ConsumptionSaveStrategyKind.OverwriteExisting,
                    };

                    break;
                }

            default:
                return;
        }

        ConsumptionSaveResultKind result = await cultivationService.SaveConsumptionAsync(input).ConfigureAwait(false);

        InfoBarMessage? message = result switch
        {
            ConsumptionSaveResultKind.NoProject => InfoBarMessage.Warning(SH.ViewModelCultivationEntryAddWarning),
            ConsumptionSaveResultKind.NoItem => InfoBarMessage.Information(SH.ViewModelCultivationConsumptionSaveNoItemHint),
            ConsumptionSaveResultKind.Added => InfoBarMessage.Success(SH.ViewModelCultivationEntryModifySuccess),
            _ => null,
        };

        await taskContext.SwitchToMainThreadAsync();

        if (message is not null)
        {
            messenger.Send(message);
        }

        CultivateProject? currentProject = Projects?.CurrentItem;

        await UpdateEntryCollectionAsync(currentProject).ConfigureAwait(false);
    }

    [Command("NavigateToPageCommand")]
    private void NavigateToPage(string? typeString)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI($"Navigate to {typeString}", "CultivationViewModel.Command"));

        if (typeString is not null)
        {
            Type? pageType = Type.GetType(typeString);
            ArgumentNullException.ThrowIfNull(pageType);
            navigationService.Navigate(pageType, NavigationExtraData.Default, true);
        }
    }

    [Command("FilterCommand")]
    private void ApplyFilter()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Filter", "CultivationViewModel.Command"));

        if (CultivateEntries is null || metadataContext is null)
        {
            return;
        }

        CultivateEntries.Filter = CultivateEntryViewFilter.Compile(SearchData, metadataContext);
        CultivateEntries.Refresh();
    }
}