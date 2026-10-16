import assert from 'node:assert/strict';
import fs from 'node:fs';
import { test } from 'node:test';

const root = new URL('../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/', import.meta.url);
const read = name => fs.readFileSync(new URL(name, root), 'utf8');
const walk = dir => fs.readdirSync(dir, { withFileTypes: true })
    .flatMap(entry => entry.isDirectory() ? walk(new URL(entry.name + '/', dir)) : [new URL(entry.name, dir)]);

test('asynchronous observable ImmutableArray properties start as empty arrays', () => {
    let count = 0;
    for (const file of walk(new URL('ViewModel/', root)).filter(file => file.pathname.endsWith('.cs'))) {
        const text = fs.readFileSync(file, 'utf8');
        for (const match of text.matchAll(/\[ObservableProperty\]\s+public partial ImmutableArray<[^\n]+> \w+ \{ get; set; \}([^\r\n]*)/g)) {
            assert.match(match[1], /= \[\];/, `${file.pathname}: ${match[0]}`);
            count++;
        }
    }
    assert.ok(count >= 10);
});

test('statistics snapshots and challenge results normalize default arrays before binding', () => {
    const abyss = read('ViewModel/Complex/HutaoSpiralAbyssDatabaseViewModel.cs');
    for (const name of ['AvatarUsageRanks', 'AvatarAppearanceRanks', 'TeamAppearances', 'AvatarConstellationInfos'])
        assert.ok(abyss.includes(`${name} = hutaoCache.${name}.EmptyIfDefault();`));
    assert.match(read('ViewModel/Complex/HutaoRoleCombatDatabaseViewModel.cs'), /AvatarAppearances = hutaoCache.AvatarAppearances.EmptyIfDefault\(\)/);
    assert.match(read('ViewModel/HardChallenge/HardChallengeViewModel.cs'), /BlingAvatars = blingAvatars.EmptyIfDefault\(\)/);
});

test('a late compiled load command is retried after the current binding update', () => {
    const behavior = read('UI/Xaml/Behavior/InvokeCommandOnLoadedBehavior.cs');
    assert.match(behavior, /PropertyChangedCallbackName = nameof\(OnCommandChanged\)/);
    assert.match(behavior, /DispatcherQueue.TryEnqueue/);
    assert.match(behavior, /target.AssociatedObject is FrameworkElement \{ IsLoaded: true \}/);
    assert.match(behavior, /if \(executed\)/);
});

test('a reattached home card restores the custom binding lifetime flag', () => {
    const lifetime = read('ViewModel/Abstraction/ViewModelSlimExtension.cs');
    assert.match(lifetime, /Loaded \+= OnFrameworkElementLoaded/);
    assert.match(lifetime, /IsViewUnloaded.Value = false/);
    assert.match(lifetime, /IsViewUnloaded.Value = true/);
    assert.doesNotMatch(lifetime, /Unloaded -= OnFrameworkElementUnloaded/);
});

test('custom material WinRT types remain eligible for AOT generation', () => {
    assert.match(read('UI/Xaml/Control/Theme/MaterialCardButton.cs'), /public partial class MaterialCardButton/);
    assert.match(read('UI/Xaml/Control/Theme/ControlMaterialState.cs'), /public sealed partial class ControlMaterialSelector/);
});

test('typed collection views expose the notifications used by CurrentItem bindings', () => {
    const contract = read('UI/Xaml/Data/IAdvancedCollectionView.cs');
    assert.match(contract, /IAdvancedCollectionView<T> : ICollectionView, INotifyPropertyChanged/);
    assert.match(read('UI/Xaml/Data/AdvancedCollectionView.cs'), /OnPropertyChanged\(nameof\(CurrentItem\)\)/);
    assert.match(contract, /new T\? CurrentItem \{ get; set; \}/);
});

test('layout stabilization never promotes arranged viewport size to a child minimum', () => {
    const sizing = read('UI/Xaml/Control/SizeRestrictedContentControl.cs');
    assert.doesNotMatch(sizing, /\.ActualHeight|\.ActualWidth|element\.MinHeight\s*=|element\.MinWidth\s*=/);
    assert.match(sizing, /Size desiredSize = base.MeasureOverride\(availableSize\)/);
    assert.match(read('UI/Xaml/Control/DesiredSizeRestriction.cs'), /double.IsFinite\(desired\)/);
    assert.match(sizing, /lastAvailableWidth.Equals\(availableSize.Width\)/);
    assert.match(sizing, /element.Visibility is Visibility.Collapsed/);
});

test('only the reward viewport is clipped, not the glass surface or hover host', () => {
    const card = read('UI/Xaml/View/Card/SignInCard.xaml');
    assert.match(card, /x:Name="AwardScrollViewer"[\s\S]*?CanContentRenderOutsideBounds="False"/);
    assert.match(card, /cw:UIElementExtensions.ClipToBounds="True"/);
    assert.doesNotMatch(read('UI/Xaml/Control/Theme/MaterialCardButton.cs'), /ClipToBounds|CreateInsetClip/);
});

test('home card hover keeps the sole surface and overlays feedback instead of replacing it', () => {
    const resources = read('UI/Xaml/Control/Theme/Card.xaml');
    const template = resources.slice(resources.indexOf('x:Key="NativeHomeCardButtonTemplate"'), resources.indexOf('x:Key="NativeHomeCardButtonStyle"'));
    assert.match(template, /Background="\{TemplateBinding Background\}"/);
    assert.match(template, /Target="StateOverlay.Opacity" Value="0.12"/);
    assert.doesNotMatch(template, /Target="[^"]*\.Background"/);
});

test('claimed reward state remains observable rather than being frozen to OneTime', () => {
    const reward = read('ViewModel/Sign/AwardView.cs');
    assert.match(reward, /AwardView : ObservableObject/);
    assert.match(reward, /\[ObservableProperty\]\s+public partial bool IsClaimed/);
    assert.match(read('UI/Xaml/View/Card/SignInCard.xaml'), /IsClaimed, Mode=OneWay/);
});

test('read-only template cleanup never freezes the shared material selection', () => {
    let count = 0;
    for (const file of walk(new URL('UI/Xaml/', root)).filter(file => file.pathname.endsWith('.xaml'))) {
        const text = fs.readFileSync(file, 'utf8');
        for (const match of text.matchAll(/\{x:Bind shuxcth:MaterialBindingSource.Current\.\w*Material[^}]*\}/g)) {
            assert.match(match[0], /Mode=OneWay/, file.pathname);
            count++;
        }
    }
    assert.ok(count > 50);
});

test('enabled state flows only from the native business control into its glass visual', () => {
    const state = read('UI/Xaml/Control/Theme/ControlMaterialState.cs');
    assert.doesNotMatch(state, /native\.IsEnabled\s*=/);
    assert.doesNotMatch(state, /OnManagedIsEnabledChanged|ManagedHooksInstalledProperty/);
    assert.match(state, /GetNativeControl\(element\) is XamlControl enabledSource/);
    assert.match(state, /Source = enabledSource,[\s\S]*?Mode = BindingMode.OneWay/);
});

test('independent profiles cover fourteen types and thirty optical slots, not control geometry', () => {
    const profiles = read('UI/Xaml/Control/Theme/LiquidGlassProfile.cs');
    const optics = read('UI/Xaml/Control/Theme/LiquidGlassOptics.cs');
    assert.equal([...profiles.matchAll(/new\("(\w+)"\),/g)].length, 14);
    assert.equal([...optics.matchAll(/new\("(\w+)", "/g)].length, 30);
    assert.doesNotMatch(optics, /b\.CornerRadius|b\.BezelWidth|b\.GlassThickness|b\.RasterizationScale/);
    assert.match(profiles, /"LiquidGlass.Profiles.v1." \+ Key/);
    assert.match(profiles, /overrides.Reset\(\)/);
    assert.doesNotMatch(read('UI/Xaml/Control/Theme/ControlMaterialState.cs'), /settings\.Strength|defaults\.Blur/);
});

test('settings open two independent windows with native TwoWay editors and a live parameter preview', () => {
    const settings = read('UI/Xaml/View/Page/SettingPage.xaml');
    const controls = read('UI/Xaml/View/Window/LiquidGlassControlsWindow.xaml');
    const effects = read('UI/Xaml/View/Window/LiquidGlassEffectsWindow.xaml');
    assert.match(settings, /Click="OpenGlassControls"/);
    assert.match(settings, /Click="OpenGlassEffects"/);
    assert.doesNotMatch(settings, /LiquidGlassParameterSettingStyle|LiquidGlassToggleSettingStyle/);
    assert.match(controls, /ToggleSwitch IsOn="\{x:Bind Enabled, Mode=TwoWay\}"/);
    assert.match(effects, /Value="\{x:Bind Value, Mode=TwoWay\}"/);
    assert.doesNotMatch(controls + effects, /<NumberBox|<CheckBox/);
    assert.match(read('UI/Xaml/View/Window/LiquidGlassEffectsWindow.xaml.cs'), /LiquidGlassPreview.Create\(profile, true\)/);
});

test('profile windows use the existing window controller and release their live subscriptions', () => {
    for (const name of ['LiquidGlassControlsWindow', 'LiquidGlassEffectsWindow']) {
        const source = read('UI/Xaml/View/Window/' + name + '.xaml.cs');
        assert.match(source, /this.InitializeController\(scope.ServiceProvider\)/);
        assert.match(source, /IXamlWindowHasInitSize/);
        assert.match(source, /TypeSelector.Items.Count > 0/);
    }
    assert.match(read('UI/Xaml/View/Window/LiquidGlassControlsWindow.xaml.cs'), /SelectionChanged -= OnSelectionChanged/);
    assert.match(read('UI/Xaml/View/Window/LiquidGlassSettingsWindows.cs'), /if \(!args.Handled\) effects = null/);
    assert.doesNotMatch(read('UI/Xaml/View/Window/LiquidGlassSettingsWindows.cs'), /\.As<.*Window/);
});

test('the appearance selector follows changes made from another settings window', () => {
    const appearance = read('ViewModel/Setting/SettingAppearanceViewModel.cs');
    assert.match(appearance, /ControlMaterial.PropertyChanged \+= OnControlMaterialChanged/);
    assert.match(appearance, /ControlMaterial.PropertyChanged -= OnControlMaterialChanged/);
    assert.doesNotMatch(appearance, /field \?\?= AppOptions.ControlMaterials/);
});

test('every new optical descriptor and profile name has Chinese and English resources', () => {
    const optics = read('UI/Xaml/Control/Theme/LiquidGlassOptics.cs');
    const profiles = read('UI/Xaml/Control/Theme/LiquidGlassProfile.cs');
    const keys = [...optics.matchAll(/new\("(\w+)", "/g)].map(match => 'GlassParameter' + match[1])
        .concat([...profiles.matchAll(/new\("(\w+)"\),/g)].map(match => 'GlassProfile' + match[1]));
    for (const file of ['Resource/Localization/SH.resx', 'Resource/Localization/SH.en.resx'])
        for (const key of keys) assert.ok(read(file).includes('name="' + key + '"'), file + ': ' + key);
});

test('queued optical refreshes cannot reconnect controls during application shutdown', () => {
    assert.match(read('UI/Xaml/Control/Theme/LiquidGlassProfile.cs'), /refreshPending \|\| XamlApplicationLifetime.Exiting/);
    assert.match(read('UI/Xaml/Control/Theme/LiquidGlassProfile.cs'), /if \(!XamlApplicationLifetime.Exiting\) ControlMaterialState.RefreshManagedOptics/);
    assert.match(read('UI/Xaml/Control/Theme/ControlMaterialState.cs'), /RefreshManagedOptics\(\)\s*\{\s*if \(XamlApplicationLifetime.Exiting\) return/);
});

test('compact achievement search keeps the transparent glass focus template', () => {
    const page = read('UI/Xaml/View/Page/AchievementPage.xaml');
    const search = page.match(/<wl:LiquidGlassSearchBox\b[\s\S]*?<\/wl:LiquidGlassSearchBox>/)[0];
    assert.doesNotMatch(search, /AutoSuggestBoxTextBoxStyle|LiquidGlassSearchBox.TextBoxStyle/);
    for (const setting of ['Height="36"', 'MinHeight="0"'])
        assert.ok(search.includes(setting));
});

test('glass combo boxes keep their own template and a safe selected-text inset', () => {
    let count = 0;
    for (const file of walk(new URL('UI/Xaml/View/', root)).filter(file => file.pathname.endsWith('.xaml'))) {
        for (const match of fs.readFileSync(file, 'utf8').matchAll(/<wl:LiquidGlassComboBox\b[^>]*>/g)) {
            if (!match[0].includes('Padding="{x:Bind')) continue;
            assert.match(match[0], /ControlMaterialState.ComboBoxPadding\(/, file.pathname);
            assert.doesNotMatch(match[0], /Style="\{ThemeResource/, file.pathname);
            count++;
        }
    }
    assert.ok(count > 30);
    assert.match(read('UI/Xaml/Control/Theme/ControlMaterialState.cs'), /Math.Max\(12, padding.Left\)/);
});

test('data pages use their own scoped glass backgrounds after XAML resource initialization', () => {
    const page = read('UI/Xaml/Control/ScopedPage.cs');
    assert.doesNotMatch(page, /MergedDictionaries.Contains\(materialResources\)|MergedDictionaries.Add\(materialResources\)/);
    const constructor = page.match(/protected ScopedPage\(\)[\s\S]*?public CancellationToken/)[0];
    assert.doesNotMatch(constructor, /MergedDictionaries|EnsureAttached|MaterialTheme.Register/);
    assert.match(page, /OnNavigatedTo\(NavigationEventArgs e\)[\s\S]*?materialResources.EnsureAttached\(Resources\)/);
    assert.match(page, /OnLoading\(FrameworkElement element, object e\)[\s\S]*?materialResources.EnsureAttached\(Resources\)/);
    assert.match(page, /static \(\) => new ResourceDictionary/);
    for (const name of ['AvatarPropertyPage', 'BackpackPage', 'CultivationPage'])
        assert.match(read('UI/Xaml/View/Page/' + name + '.xaml'), /Background="\{ThemeResource HutaoCardBrush\}"/);
});

test('mutable presentation models and their calculated values have notification contracts', () => {
    for (const file of ['ViewModel/AvatarProperty/AvatarView.cs', 'ViewModel/GachaLog/SummaryItem.cs',
        'ViewModel/Backpack/BackpackReliquaryItemView.cs', 'ViewModel/Cultivation/StatisticsCultivateItem.cs',
        'ViewModel/SpiralAbyss/BattleView.cs', 'Service/Notification/InfoBarOptions.cs'])
        assert.match(read(file), /\[ObservableProperty\]/, file);
    assert.match(read('ViewModel/AvatarProperty/AvatarView.cs'), /PropertyChanged \+= OnConstellationPropertyChanged/);
    assert.match(read('ViewModel/Backpack/BackpackReliquaryItemView.cs'), /NotifyPropertyChangedFor\(nameof\(DisplayScore\)\)/);
    assert.match(read('ViewModel/Cultivation/StatisticsCultivateItem.cs'), /NotifyPropertyChangedFor\(nameof\(IsFinished\)\)/);
    assert.match(read('ViewModel/GachaLog/Countdown.cs'), /Histories.CollectionChanged \+= OnHistoriesChanged/);
    assert.doesNotMatch(read('Snap.Hutao.Remastered.csproj'), /NoWarn[^<]*WMC1506/);
});
