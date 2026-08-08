// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Service.AvatarInfo.Factory;
using Snap.Hutao.Remastered.Service.Backpack;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Yae.PlayerStore;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.ViewModel.Game;

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

[Service(ServiceLifetime.Scoped)]
public sealed partial class BackpackViewModel : Abstraction.ViewModel
{
    private readonly BackpackViewModelScopeContext scopeContext;
    private readonly ExclusiveTokenProvider itemsTokenProvider = new();
    private ImmutableDictionary<BackpackItemCategory, ImmutableArray<BackpackItemView>> categoryItems = [];
    private FrozenDictionary<uint, int> foodQualityMap = FrozenDictionary<uint, int>.Empty;
    private FrozenDictionary<uint, CookFoodType> foodTypeMap = FrozenDictionary<uint, CookFoodType>.Empty;
    private ImmutableDictionary<BackpackItemCategory, FrozenDictionary<string, SearchToken>> categoryTokens = [];
    private ImmutableDictionary<BackpackItemCategory, ImmutableArray<AutoSortToken>> categorySortTokens = [];

    private static readonly ImmutableArray<BackpackItemCategory> CategoryIndexMap = [
        BackpackItemCategory.Weapon,
        BackpackItemCategory.Reliquary,
        BackpackItemCategory.UpgradeItem,
        BackpackItemCategory.Food,
        BackpackItemCategory.Material,
        BackpackItemCategory.Gadget,
        BackpackItemCategory.Quest,
        BackpackItemCategory.PreciousItem,
        BackpackItemCategory.Furniture,
    ];

    [GeneratedConstructor]
    public partial BackpackViewModel(IServiceProvider serviceProvider);

    public IAdvancedDbCollectionView<BackpackArchive>? Archives
    {
        get;
        set
        {
            AdvancedCollectionViewCurrentChanged.Detach(field, OnCurrentArchiveChanged);
            SetProperty(ref field, value);
            AdvancedCollectionViewCurrentChanged.Attach(value, OnCurrentArchiveChanged);
        }
    }

    [ObservableProperty]
    public partial ImmutableArray<BackpackItemView> Items { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedCategoryIndex { get; set; }

    [ObservableProperty]
    public partial SearchData? SearchData { get; set; }

    [ObservableProperty]
    public partial double? FilterLevel { get; set; }

    [ObservableProperty]
    public partial ImmutableArray<AutoSortToken> AvailableSortTokens { get; set; } = [];

    public BackpackReliquaryScoreConfig? ScoreConfig { get; private set; }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        // Set empty SearchData so AutoSuggestTokenBox has a non-null binding target
        SearchData = SearchData.Create(FrozenDictionary<string, SearchToken>.Empty);

        if (!await scopeContext.MetadataService.InitializeAsync().ConfigureAwait(false))
        {
            return false;
        }

        token.ThrowIfCancellationRequested();

        IAdvancedDbCollectionView<BackpackArchive> archives;
        using (await EnterCriticalSectionAsync().ConfigureAwait(false))
        {
            archives = await scopeContext.BackpackService.GetArchiveCollectionAsync().ConfigureAwait(false);
        }

        await scopeContext.TaskContext.SwitchToMainThreadAsync();

        Archives = archives;
        Archives.MoveCurrentTo(Archives.Source.SelectedOrFirstOrDefault());

        ScoreConfig = scopeContext.BackpackService.GetActiveReliquaryScoreConfig();
        UpdateItemsAsync(Archives.CurrentItem, itemsTokenProvider.GetNewToken()).SafeForget();

        return true;
    }

    protected override void UninitializeOverride()
    {
        using (Archives?.SuppressChangeCurrentItem())
        {
            Archives = default;
        }

        Items = [];
    }

    private void OnCurrentArchiveChanged(object? sender, object? e)
    {
        UpdateItemsAsync(Archives?.CurrentItem, itemsTokenProvider.GetNewToken()).SafeForget();
    }

    partial void OnSelectedCategoryIndexChanged(int value)
    {
        BackpackItemCategory category = GetSelectedCategory();
        BuildSearchData(category);
        BuildSortTokens(category);
        UpdateItemsFilter(category);
    }

    partial void OnFilterLevelChanged(double? value)
    {
        UpdateItemsFilter(GetSelectedCategory());
    }

    [Command("ApplySortCommand")]
    private void ApplySort()
    {
        UpdateItemsFilter(GetSelectedCategory());
    }

    [Command("AddArchiveCommand")]
    private async Task AddArchiveAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Add archive", "BackpackViewModel.Command"));

        if (Archives is null)
        {
            return;
        }

        BackpackArchiveCreateDialog dialog = await scopeContext.ContentDialogFactory.CreateInstanceAsync<BackpackArchiveCreateDialog>(scopeContext.ServiceProvider).ConfigureAwait(false);
        if (await dialog.GetInputAsync().ConfigureAwait(false) is not (true, { } name))
        {
            return;
        }

        BackpackArchive added = scopeContext.BackpackService.AddArchive(name);

        IAdvancedDbCollectionView<BackpackArchive> archives = await scopeContext.BackpackService.GetArchiveCollectionAsync().ConfigureAwait(false);
        await scopeContext.TaskContext.SwitchToMainThreadAsync();
        Archives = archives;

        BackpackArchive? current = Archives.Source.FirstOrDefault(a => a.InnerId == added.InnerId);
        Archives.MoveCurrentTo(current ?? Archives.Source.FirstOrDefault());

        scopeContext.Messenger.Send(InfoBarMessage.Success(SH.FormatViewPageBackpackArchiveAdded(name)));
    }

    [Command("RemoveArchiveCommand")]
    private async Task RemoveArchiveAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove archive", "BackpackViewModel.Command"));

        if (Archives?.CurrentItem is not { } current)
        {
            return;
        }

        ContentDialogResult result = await scopeContext.ContentDialogFactory
            .CreateForConfirmCancelAsync(
                SH.FormatViewPageBackpackRemoveArchiveTitle(current.Name),
                SH.ViewPageBackpackRemoveArchiveContent)
            .ConfigureAwait(false);

        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            using (await EnterCriticalSectionAsync().ConfigureAwait(false))
            {
                await scopeContext.BackpackService.RemoveArchiveAsync(current).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }

        IAdvancedDbCollectionView<BackpackArchive> archives = await scopeContext.BackpackService.GetArchiveCollectionAsync().ConfigureAwait(false);
        await scopeContext.TaskContext.SwitchToMainThreadAsync();
        Archives = archives;
        Archives.MoveCurrentTo(Archives.Source.SelectedOrFirstOrDefault());
    }

    [Command("ConfigureScoreCommand")]
    private async Task ConfigureScoreAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("ConfigureScore", "BackpackViewModel.Command"));

        BackpackReliquaryScoreConfigDialog dialog = await scopeContext.ContentDialogFactory
            .CreateInstanceAsync<BackpackReliquaryScoreConfigDialog>(scopeContext.ServiceProvider)
            .ConfigureAwait(false);

        ImmutableArray<BackpackReliquaryScoreConfig> allConfigs = scopeContext.BackpackService.GetAllReliquaryScoreConfigs();
        BackpackReliquaryScoreConfig activeConfig = ScoreConfig ?? scopeContext.BackpackService.GetActiveReliquaryScoreConfig();

        BackpackReliquaryScoreConfig? result = await dialog.GetInputAsync(
            allConfigs,
            activeConfig,
            scopeContext.BackpackService.CreatePreset,
            scopeContext.BackpackService.DeleteReliquaryScoreConfig).ConfigureAwait(false);
        if (result is null)
        {
            return;
        }

        result.IsActive = true;
        result = scopeContext.BackpackService.SaveReliquaryScoreConfig(result);
        ScoreConfig = result;

        // Refresh all items to update scores with the saved config
        await UpdateItemsAsync(Archives?.CurrentItem, itemsTokenProvider.GetNewToken()).ConfigureAwait(false);
    }

    [Command("RefreshByEmbeddedYaeCommand")]
    private async Task RefreshByEmbeddedYaeAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Refresh backpack", "BackpackViewModel.Command", [("source", "Embedded Yae")]));

        if (!HutaoRuntime.IsProcessElevated)
        {
            await scopeContext.ContentDialogFactory
                .CreateForConfirmAsync(SH.ViewModelYaeProcessNotElevatedTitle, SH.ViewModelYaeProcessNotElevatedDescription)
                .ConfigureAwait(false);
            return;
        }

        if (Archives?.CurrentItem is not { } archive)
        {
            return;
        }

        EmbeddedYaeLaunchExecutionViewModel viewModel = scopeContext.ServiceProvider.GetRequiredService<EmbeddedYaeLaunchExecutionViewModel>();
        if (!await viewModel.InitializeAsync().ConfigureAwait(false))
        {
            return;
        }

        PlayerStoreResult? storeResult = await scopeContext.YaeService.GetPlayerStoreResultAsync(viewModel).ConfigureAwait(false);

        if (storeResult is null)
        {
            scopeContext.Messenger.Send(InfoBarMessage.Warning(SH.ViewPageBackpackRefreshWarning));
            return;
        }

        if (await scopeContext.BackpackService.RefreshByEmbeddedYaeAsync(archive, storeResult).ConfigureAwait(false))
        {
            scopeContext.Messenger.Send(InfoBarMessage.Success(SH.ViewPageBackpackRefreshSuccess));
        }
        else
        {
            scopeContext.Messenger.Send(InfoBarMessage.Warning(SH.ViewPageBackpackRefreshWarning));
        }

        await UpdateItemsAsync(archive, itemsTokenProvider.GetNewToken()).ConfigureAwait(false);
    }

    private async ValueTask UpdateItemsAsync(BackpackArchive? archive, CancellationToken token)
    {
        await scopeContext.TaskContext.InvokeOnMainThreadAsync(() => Items = []).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        if (archive is null)
        {
            categoryItems = [];
            return;
        }

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken);
        BackpackServiceMetadataContext context = await scopeContext.MetadataService
            .GetContextAsync<BackpackServiceMetadataContext>(linkedCts.Token)
            .ConfigureAwait(false);

        context.ReliquaryScoreConfig = scopeContext.BackpackService.GetActiveReliquaryScoreConfig();
        ScoreConfig = context.ReliquaryScoreConfig;

        ImmutableArray<BackpackItemView> allItems = [.. scopeContext.BackpackService
            .GetBackpackItemImmutableArrayByArchiveId(archive.InnerId)
            .Select(item => BackpackItemView.Create(item, context))];

        // Calculate scores for reliquary items
        foreach (BackpackItemView item in allItems)
        {
            if (item is BackpackReliquaryItemView relicItem)
            {
                relicItem.Score = ReliquaryScoreCalculator.CalculateWithWeights(relicItem.SubStats.Select(s => (s.FightProp, s.Value)), context.ReliquaryScoreConfig.GetWeight);
            }
        }

        categoryItems = BuildCategoryViews(allItems);

        // Build food quality/type reverse lookup maps
        Dictionary<uint, int> qualityMap = [];
        Dictionary<uint, CookFoodType> typeMap = [];
        foreach (ref readonly CookRecipe recipe in context.CookRecipes.AsSpan())
        {
            ImmutableArray<IdCount> outputs = recipe.QualityOutput;
            for (int i = 0; i < outputs.Length; i++)
            {
                qualityMap.TryAdd(outputs[i].Id, i);
                typeMap.TryAdd(outputs[i].Id, recipe.FoodType);
            }
        }

        foodQualityMap = qualityMap.ToFrozenDictionary();
        foodTypeMap = typeMap.ToFrozenDictionary();

        // Pre-build token dictionaries for all categories (on background thread)
        ImmutableDictionary<BackpackItemCategory, FrozenDictionary<string, SearchToken>>.Builder tokenBuilder =
            ImmutableDictionary.CreateBuilder<BackpackItemCategory, FrozenDictionary<string, SearchToken>>();
        foreach (BackpackItemCategory cat in Enum.GetValues<BackpackItemCategory>())
        {
            ImmutableArray<BackpackItemView> catItems = categoryItems.GetValueOrDefault(cat, []);
            tokenBuilder.Add(cat, BackpackFilterTokenBuilder.Build(cat, catItems));
        }

        categoryTokens = tokenBuilder.ToImmutable();
        categorySortTokens = BackpackSortTokenBuilder.Build();

        await scopeContext.TaskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();

        BackpackItemCategory category = GetSelectedCategory();
        BuildSearchData(category);
        BuildSortTokens(category);
        UpdateItemsFilter(category);
    }

    [Command("FilterCommand")]
    private void ApplyFilter()
    {
        UpdateItemsFilter(GetSelectedCategory());
    }

    private BackpackItemCategory GetSelectedCategory()
    {
        int index = SelectedCategoryIndex;
        return index >= 0 && index < CategoryIndexMap.Length
            ? CategoryIndexMap[index]
            : BackpackItemCategory.Weapon;
    }

    private void BuildSearchData(BackpackItemCategory category)
    {
        SearchData = SearchData.Create(categoryTokens.GetValueOrDefault(category, FrozenDictionary<string, SearchToken>.Empty));
    }

    private void BuildSortTokens(BackpackItemCategory category)
    {
        if (categorySortTokens.TryGetValue(category, out ImmutableArray<AutoSortToken> tokens))
        {
            // Reset selection state on all tokens
            foreach (AutoSortToken token in tokens)
            {
                token.IsSelected = false;
                token.SortOrder = 0;
                token.IsDescending = true;
            }

            AvailableSortTokens = tokens;
        }
    }

    private void UpdateItemsFilter(BackpackItemCategory category)
    {
        ImmutableArray<BackpackItemView> items = categoryItems.GetValueOrDefault(category, []);
        Predicate<BackpackItemView>? predicate = BackpackFilter.Compile(SearchData, FilterLevel, foodQualityMap, foodTypeMap);
        ImmutableArray<BackpackItemView> filtered = predicate is null ? items : [.. items.Where(item => predicate(item))];

        // Items in categoryItems are already sorted with default sort; only re-sort when custom sort is active
        IComparer<BackpackItemView>? comparer = new AutoSortData<BackpackItemView>(AvailableSortTokens, BackpackSortComparer.CompareByKind).Compile();
        Items = comparer is null ? filtered : [.. filtered.OrderBy(x => x, comparer)];
    }

    private static ImmutableArray<BackpackItemView> ApplyDefaultSort(ImmutableArray<BackpackItemView> items, BackpackItemCategory category)
    {
        return category switch
        {
            BackpackItemCategory.Weapon => [.. items
                .Cast<BackpackWeaponItemView>()
                .OrderByDescending(w => w.Weapon.RankLevel)
                .ThenByDescending(w => w.Level)
                .ThenBy(w => w.Entity.ItemId)],
            BackpackItemCategory.Reliquary => [.. items
                .Cast<BackpackReliquaryItemView>()
                .OrderByDescending(r => r.Level)
                .ThenBy(r => r.Entity.ItemId)],
            _ => [.. items
                .OrderByDescending(GetRank)
                .ThenBy(item => item.Entity.ItemId)],
        };
    }

    private static uint GetRank(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView w)
        {
            QualityType rank = w.Weapon.RankLevel;
            return Unsafe.As<QualityType, uint>(ref rank);
        }

        if (item.Material is not null)
        {
            QualityType rank = item.Material.RankLevel;
            return Unsafe.As<QualityType, uint>(ref rank);
        }

        return 1;
    }

    private static ImmutableDictionary<BackpackItemCategory, ImmutableArray<BackpackItemView>> BuildCategoryViews(ImmutableArray<BackpackItemView> all)
    {
        ImmutableDictionary<BackpackItemCategory, ImmutableArray<BackpackItemView>>.Builder builder =
            ImmutableDictionary.CreateBuilder<BackpackItemCategory, ImmutableArray<BackpackItemView>>();

        foreach (BackpackItemCategory cat in Enum.GetValues<BackpackItemCategory>())
        {
            ImmutableArray<BackpackItemView> items = [.. all
                .Where(item => item.Category == cat && IsCorrectType(item, cat))];

            builder.Add(cat, ApplyDefaultSort(items, cat));
        }

        return builder.ToImmutable();
    }

    private static bool IsCorrectType(BackpackItemView item, BackpackItemCategory category)
    {
        return category switch
        {
            BackpackItemCategory.Weapon => item is BackpackWeaponItemView,
            BackpackItemCategory.Reliquary => item is BackpackReliquaryItemView,
            _ => item is not BackpackWeaponItemView and not BackpackReliquaryItemView,
        };
    }
}
