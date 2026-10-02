// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class BackpackReliquaryScoreConfigDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    private ImmutableArray<BackpackReliquaryScoreConfig> savedConfigs = [];
    private readonly List<Guid> deletedConfigIds = [];
    private List<PresetComboItem> comboItems = [];
    private PresetComboItem? selectedComboItem;
    private BackpackReliquaryScoreConfig currentConfig = default!;
    private bool isUpdating;

    private Func<ReliquaryScoreConfigPreset, BackpackReliquaryScoreConfig> createPreset = default!;
    private Action<Guid>? deleteCallback;

    private static readonly ReliquaryScoreConfigPreset[] BuiltInPresets =
    [
        ReliquaryScoreConfigPreset.Default,
        ReliquaryScoreConfigPreset.ATKScaler,
        ReliquaryScoreConfigPreset.HPScaler,
        ReliquaryScoreConfigPreset.DEFScaler,
        ReliquaryScoreConfigPreset.EM,
    ];

    [GeneratedConstructor(InitializeComponent = true)]
    public partial BackpackReliquaryScoreConfigDialog(IServiceProvider serviceProvider);

    public async ValueTask<BackpackReliquaryScoreConfig?> GetInputAsync(
        ImmutableArray<BackpackReliquaryScoreConfig> allConfigs,
        BackpackReliquaryScoreConfig activeConfig,
        Func<ReliquaryScoreConfigPreset, BackpackReliquaryScoreConfig> createPresetCallback,
        Action<Guid> deleteConfigCallback)
    {
        savedConfigs = allConfigs;
        currentConfig = activeConfig.Clone();
        createPreset = createPresetCallback;
        deleteCallback = deleteConfigCallback;
        comboItems = BuildComboItems();
        PresetComboBox.ItemsSource = comboItems;
        BindConfigToControls();

        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();

        if (result is ContentDialogResult.Primary)
        {
            // 删除只在用户确认后才落库：取消时对话框不应留下任何持久化副作用，
            // 否则调用方会持有已被删除的配置（下拉、缓存权重、角色设置都成了旧数据）
            foreach (Guid deletedConfigId in deletedConfigIds)
            {
                deleteCallback?.Invoke(deletedConfigId);
            }

            currentConfig.IsActive = true;
            return currentConfig;
        }

        return null;
    }

    private static string GetConfigDisplayName(BackpackReliquaryScoreConfig config)
    {
        if (!string.IsNullOrEmpty(config.Name))
        {
            return config.Name;
        }

        return config.PresetKey.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture) ?? config.PresetKey.ToString();
    }

    private List<PresetComboItem> BuildComboItems()
    {
        List<PresetComboItem> items = [];

        foreach (ReliquaryScoreConfigPreset preset in BuiltInPresets)
        {
            string name = preset.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture) ?? preset.ToString();
            items.Add(new PresetComboItem { DisplayName = name, PresetKey = preset });
        }

        foreach (BackpackReliquaryScoreConfig saved in savedConfigs)
        {
            // 已落库的配置一律列出，否则用户既看不到也无法选中、重命名或删除它
            string name = string.IsNullOrEmpty(saved.Name)
                ? saved.PresetKey.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture) ?? saved.PresetKey.ToString()
                : saved.Name;
            items.Add(new PresetComboItem { DisplayName = name, ConfigId = saved.InnerId });
        }

        return items;
    }

    private void BindConfigToControls()
    {
        isUpdating = true;

        CritSlider.Value = currentConfig.CritWeight;
        CritHurtSlider.Value = currentConfig.CritHurtWeight;
        AttackPercentSlider.Value = currentConfig.AttackPercentWeight;
        ChargeEfficiencySlider.Value = currentConfig.ChargeEfficiencyWeight;
        ElementalMasterySlider.Value = currentConfig.ElementalMasteryWeight;
        HpPercentSlider.Value = currentConfig.HpPercentWeight;
        DefensePercentSlider.Value = currentConfig.DefensePercentWeight;

        UpdateAllLabels();

        if (currentConfig.PresetKey is ReliquaryScoreConfigPreset.Custom && string.IsNullOrEmpty(currentConfig.Name))
        {
            NameTextBox.Text = string.Empty;
        }
        else
        {
            NameTextBox.Text = GetConfigDisplayName(currentConfig);
        }

        // 用户刚点选的那一行优先：编辑已有配置时再选内置预设，currentConfig 仍带着原配置的 InnerId，
        // 若直接按 InnerId 反查会把下拉选回原配置行，用户刚选的预设看起来被弹了回去。
        // 同时把 selectedComboItem 同步成实际显示的行，删除按钮才有明确的操作对象
        PresetComboItem? match = selectedComboItem ?? ResolveComboItem();
        selectedComboItem = match;

        DeleteButton.Visibility = match?.ConfigId.HasValue == true ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        PresetComboBox.SelectedItem = match;
        isUpdating = false;
    }

    /// <summary>
    /// 未指定选中行时按当前配置反查：先按 InnerId 精确匹配已保存的配置，再退回按预设匹配内置预设行。
    /// 顺序反过来的话，内置预设行会抢先命中，使用了内置预设的已保存配置就无法被选中和删除
    /// </summary>
    private PresetComboItem? ResolveComboItem()
    {
        return comboItems.Find(item => item.ConfigId.HasValue && item.ConfigId == currentConfig.InnerId)
            ?? comboItems.Find(item => item.PresetKey.HasValue && item.PresetKey == currentConfig.PresetKey && currentConfig.PresetKey != ReliquaryScoreConfigPreset.Custom);
    }

    private void UpdateAllLabels()
    {
        CritLabel.Text = currentConfig.CritWeight.ToString("F1", CultureInfo.CurrentCulture);
        CritHurtLabel.Text = currentConfig.CritHurtWeight.ToString("F1", CultureInfo.CurrentCulture);
        AttackPercentLabel.Text = currentConfig.AttackPercentWeight.ToString("F1", CultureInfo.CurrentCulture);
        ChargeEfficiencyLabel.Text = currentConfig.ChargeEfficiencyWeight.ToString("F1", CultureInfo.CurrentCulture);
        ElementalMasteryLabel.Text = currentConfig.ElementalMasteryWeight.ToString("F1", CultureInfo.CurrentCulture);
        HpPercentLabel.Text = currentConfig.HpPercentWeight.ToString("F1", CultureInfo.CurrentCulture);
        DefensePercentLabel.Text = currentConfig.DefensePercentWeight.ToString("F1", CultureInfo.CurrentCulture);
    }

    private void OnPresetComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isUpdating || PresetComboBox.SelectedItem is not PresetComboItem item)
        {
            return;
        }

        selectedComboItem = item;
        Guid existingInnerId = currentConfig.InnerId;

        if (item.ConfigId.HasValue)
        {
            BackpackReliquaryScoreConfig? saved = savedConfigs.FirstOrDefault(c => c.InnerId == item.ConfigId.Value);
            if (saved is not null)
            {
                currentConfig = saved.Clone();
            }
        }
        else if (item.PresetKey.HasValue)
        {
            currentConfig = createPreset(item.PresetKey.Value);
            currentConfig.InnerId = existingInnerId;
        }

        BindConfigToControls();
    }

    private void OnSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (isUpdating)
        {
            return;
        }

        double value = Math.Round(e.NewValue, 1);
        (TextBlock label, Action<double> setter) = GetSliderBinding(sender);
        label.Text = value.ToString("F1", CultureInfo.CurrentCulture);
        setter(value);

        currentConfig.PresetKey = ReliquaryScoreConfigPreset.Custom;
    }

    private (TextBlock Label, Action<double> Setter) GetSliderBinding(object sender)
    {
        return sender switch
        {
            _ when ReferenceEquals(sender, CritSlider) => (CritLabel, v => currentConfig.CritWeight = v),
            _ when ReferenceEquals(sender, CritHurtSlider) => (CritHurtLabel, v => currentConfig.CritHurtWeight = v),
            _ when ReferenceEquals(sender, AttackPercentSlider) => (AttackPercentLabel, v => currentConfig.AttackPercentWeight = v),
            _ when ReferenceEquals(sender, ChargeEfficiencySlider) => (ChargeEfficiencyLabel, v => currentConfig.ChargeEfficiencyWeight = v),
            _ when ReferenceEquals(sender, ElementalMasterySlider) => (ElementalMasteryLabel, v => currentConfig.ElementalMasteryWeight = v),
            _ when ReferenceEquals(sender, HpPercentSlider) => (HpPercentLabel, v => currentConfig.HpPercentWeight = v),
            _ when ReferenceEquals(sender, DefensePercentSlider) => (DefensePercentLabel, v => currentConfig.DefensePercentWeight = v),
            _ => throw new ArgumentException("Unknown slider", nameof(sender)),
        };
    }

    private void OnNewConfigClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        selectedComboItem = null;
        currentConfig = new BackpackReliquaryScoreConfig
        {
            PresetKey = ReliquaryScoreConfigPreset.Custom,
        };

        BindConfigToControls();
    }

    private void OnNameTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isUpdating)
        {
            return;
        }

        currentConfig.Name = NameTextBox.Text;
    }

    private void OnDeleteConfigClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (selectedComboItem?.ConfigId is not { } configId || configId == Guid.Empty)
        {
            return;
        }

        // 仅从对话框的列表里移除，真正的删除推迟到用户点击确定
        deletedConfigIds.Add(configId);
        savedConfigs = savedConfigs.Where(c => c.InnerId != configId).ToImmutableArray();
        comboItems = BuildComboItems();
        PresetComboBox.ItemsSource = comboItems;
        selectedComboItem = null;

        currentConfig = createPreset(ReliquaryScoreConfigPreset.Default);
        BindConfigToControls();
    }
}
