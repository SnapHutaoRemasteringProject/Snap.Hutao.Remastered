// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    private bool isClosing;

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

        isClosing = false;
        Closing -= OnDialogClosing;
        Closing += OnDialogClosing;

        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();

        if (result is ContentDialogResult.Primary)
        {
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

        SetSliderValue(CritSlider, CritSliderGlass, currentConfig.CritWeight);
        SetSliderValue(CritHurtSlider, CritHurtSliderGlass, currentConfig.CritHurtWeight);
        SetSliderValue(AttackPercentSlider, AttackPercentSliderGlass, currentConfig.AttackPercentWeight);
        SetSliderValue(ChargeEfficiencySlider, ChargeEfficiencySliderGlass, currentConfig.ChargeEfficiencyWeight);
        SetSliderValue(ElementalMasterySlider, ElementalMasterySliderGlass, currentConfig.ElementalMasteryWeight);
        SetSliderValue(HpPercentSlider, HpPercentSliderGlass, currentConfig.HpPercentWeight);
        SetSliderValue(DefensePercentSlider, DefensePercentSliderGlass, currentConfig.DefensePercentWeight);

        UpdateAllLabels();

        if (currentConfig.PresetKey is ReliquaryScoreConfigPreset.Custom && string.IsNullOrEmpty(currentConfig.Name))
        {
            NameTextBox.Text = string.Empty;
        }
        else
        {
            NameTextBox.Text = GetConfigDisplayName(currentConfig);
        }

        PresetComboItem? match = selectedComboItem ?? ResolveComboItem();
        selectedComboItem = match;

        DeleteButton.Visibility = match?.ConfigId.HasValue == true ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        PresetComboBox.SelectedItem = match;
        isUpdating = false;
    }

    private static void SetSliderValue(Slider native, Slider glass, double value)
    {
        native.Value = value;
        glass.Value = value;
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
        if (isUpdating || isClosing)
        {
            return;
        }

        double value = Math.Round(e.NewValue, 1);
        (TextBlock label, Action<double> setter, Slider peer) = GetSliderBinding(sender);
        label.Text = value.ToString("F1", CultureInfo.CurrentCulture);
        setter(value);

        // MaterialSwap keeps both instances alive but only one is in the visual tree.
        // Sync the hidden peer so switching material does not reveal a stale value.
        if (!ReferenceEquals(peer, sender) && peer.Value != value)
        {
            isUpdating = true;
            try
            {
                peer.Value = value;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // A closing ContentDialog can detach the hidden peer while its value is
                // still being written. Do not let that escape a UI event handler: it
                // would leave ContentDialog.ShowAsync pending and block the next dialog.
                Debug.WriteLine($"Reliquary score slider sync failed: {ex}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        currentConfig.PresetKey = ReliquaryScoreConfigPreset.Custom;
    }

    private void OnDialogClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        isClosing = true;
    }

    private (TextBlock Label, Action<double> Setter, Slider Peer) GetSliderBinding(object sender)
    {
        return sender switch
        {
            _ when ReferenceEquals(sender, CritSlider) => (CritLabel, v => currentConfig.CritWeight = v, CritSliderGlass),
            _ when ReferenceEquals(sender, CritSliderGlass) => (CritLabel, v => currentConfig.CritWeight = v, CritSlider),
            _ when ReferenceEquals(sender, CritHurtSlider) => (CritHurtLabel, v => currentConfig.CritHurtWeight = v, CritHurtSliderGlass),
            _ when ReferenceEquals(sender, CritHurtSliderGlass) => (CritHurtLabel, v => currentConfig.CritHurtWeight = v, CritHurtSlider),
            _ when ReferenceEquals(sender, AttackPercentSlider) => (AttackPercentLabel, v => currentConfig.AttackPercentWeight = v, AttackPercentSliderGlass),
            _ when ReferenceEquals(sender, AttackPercentSliderGlass) => (AttackPercentLabel, v => currentConfig.AttackPercentWeight = v, AttackPercentSlider),
            _ when ReferenceEquals(sender, ChargeEfficiencySlider) => (ChargeEfficiencyLabel, v => currentConfig.ChargeEfficiencyWeight = v, ChargeEfficiencySliderGlass),
            _ when ReferenceEquals(sender, ChargeEfficiencySliderGlass) => (ChargeEfficiencyLabel, v => currentConfig.ChargeEfficiencyWeight = v, ChargeEfficiencySlider),
            _ when ReferenceEquals(sender, ElementalMasterySlider) => (ElementalMasteryLabel, v => currentConfig.ElementalMasteryWeight = v, ElementalMasterySliderGlass),
            _ when ReferenceEquals(sender, ElementalMasterySliderGlass) => (ElementalMasteryLabel, v => currentConfig.ElementalMasteryWeight = v, ElementalMasterySlider),
            _ when ReferenceEquals(sender, HpPercentSlider) => (HpPercentLabel, v => currentConfig.HpPercentWeight = v, HpPercentSliderGlass),
            _ when ReferenceEquals(sender, HpPercentSliderGlass) => (HpPercentLabel, v => currentConfig.HpPercentWeight = v, HpPercentSlider),
            _ when ReferenceEquals(sender, DefensePercentSlider) => (DefensePercentLabel, v => currentConfig.DefensePercentWeight = v, DefensePercentSliderGlass),
            _ when ReferenceEquals(sender, DefensePercentSliderGlass) => (DefensePercentLabel, v => currentConfig.DefensePercentWeight = v, DefensePercentSlider),
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

        deletedConfigIds.Add(configId);
        savedConfigs = savedConfigs.Where(c => c.InnerId != configId).ToImmutableArray();
        comboItems = BuildComboItems();
        PresetComboBox.ItemsSource = comboItems;
        selectedComboItem = null;

        currentConfig = createPreset(ReliquaryScoreConfigPreset.Default);
        BindConfigToControls();
    }
}
