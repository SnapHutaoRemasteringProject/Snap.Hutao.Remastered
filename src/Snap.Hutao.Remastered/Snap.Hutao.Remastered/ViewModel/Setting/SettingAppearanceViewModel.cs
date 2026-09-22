// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Factory.Picker;

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.BackgroundImage;
using Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.UI.Xaml.Media.Backdrop;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingAppearanceViewModel : Abstraction.ViewModel
{
    [GeneratedConstructor]
    public partial SettingAppearanceViewModel(IServiceProvider serviceProvider);

    public partial CultureOptions CultureOptions { get; }

    public partial AppOptions AppOptions { get; }

    public partial BackgroundImageOptions BackgroundImageOptions { get; }

    public partial IMessenger Messenger { get; }

    public int ControlMaterialState => (int)AppOptions.ControlMaterial.Value;

    // TODO: Replace with IObservableProperty
    public NameValue<ControlMaterial>? SelectedControlMaterial
    {
        get => field ??= AppOptions.ControlMaterials.Single(material => material.Value == AppOptions.ControlMaterial.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.ControlMaterial.Value = value.Value;
                MaterialTheme.Apply(value.Value);
                OnPropertyChanged(nameof(ControlMaterialState));
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<BackgroundMediaType>? SelectedBackgroundMediaType
    {
        get => field ??= Selection.Initialize(AppOptions.BackgroundMediaTypes, AppOptions.BackgroundMediaType.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.BackgroundMediaType.Value = value.Value;
                Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
            }
        }
    }

    public string? BackgroundMediaPath
    {
        get => AppOptions.BackgroundMediaPath.Value;
        set
        {
            if (AppOptions.BackgroundMediaPath.Value == value)
            {
                return;
            }

            AppOptions.BackgroundMediaPath.Value = value ?? string.Empty;
            OnPropertyChanged();
            Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
        }
    }

    public bool IsLooping
    {
        get => AppOptions.IsBackgroundMediaLooping.Value;
        set
        {
            if (AppOptions.IsBackgroundMediaLooping.Value == value) return;
            AppOptions.IsBackgroundMediaLooping.Value = value;
            OnPropertyChanged();
            Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
        }
    }

    public bool IsMuted
    {
        get => AppOptions.IsBackgroundMediaMuted.Value;
        set
        {
            if (AppOptions.IsBackgroundMediaMuted.Value == value) return;
            AppOptions.IsBackgroundMediaMuted.Value = value;
            OnPropertyChanged();
            Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
        }
    }

    [Command("SetBackgroundMediaFolderCommand")]
    private async Task SetBackgroundMediaFolderAsync()
    {
        ValueResult<bool, string?> result = FileSystemPickerInteraction.PickFolder(SH.ViewPageSettingBackgroundVideoPickFolderTitle);
        if (result.TryGetValue(out string? path))
        {
            await TaskContext.SwitchToMainThreadAsync();
            AppOptions.BackgroundMediaPath.Value = path;
            OnPropertyChanged(nameof(BackgroundMediaPath));
            Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
        }
    }

    [Command("ResetBackgroundMediaFolderCommand")]
    private void ResetBackgroundMediaFolder()
    {
        AppOptions.BackgroundMediaPath.Value = string.Empty;
        OnPropertyChanged(nameof(BackgroundMediaPath));
        Messenger.Send(new Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage());
    }

    public partial IFileSystemPickerInteraction FileSystemPickerInteraction { get; }

    public partial ITaskContext TaskContext { get; }

    // TODO: Replace with IObservableProperty
    public NameCultureInfoValue? SelectedCulture
    {
        get => field ??= Selection.Initialize(CultureOptions.Cultures, CultureOptions.CurrentCulture.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                CultureOptions.CurrentCulture.Value = value.Value;
                AppInstance.Restart(string.Empty);
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<DayOfWeek>? SelectedFirstDayOfWeek
    {
        get => field ??= CultureOptions.DayOfWeeks.FirstOrDefault(d => d.Value == CultureOptions.FirstDayOfWeek.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                CultureOptions.FirstDayOfWeek.Value = value.Value;
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<BackdropType>? SelectedBackdropType
    {
        get => field ??= AppOptions.BackdropTypes.Single(t => t.Value == AppOptions.BackdropType.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.BackdropType.Value = value.Value;
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<ElementTheme>? SelectedElementTheme
    {
        get => field ??= AppOptions.LazyElementThemes.Value.Single(t => t.Value == AppOptions.ElementTheme.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.ElementTheme.Value = value.Value;
                FrameworkTheming.SetTheme(ThemeHelper.ElementToFramework(value.Value));
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<BackgroundImageType>? SelectedBackgroundImageType
    {
        get => field ??= AppOptions.BackgroundImageTypes.Single(t => t.Value == AppOptions.BackgroundImageType.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.BackgroundImageType.Value = value.Value;
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<LastWindowCloseBehavior>? SelectedLastWindowCloseBehavior
    {
        get => field ??= AppOptions.LastWindowCloseBehaviors.Single(t => t.Value == AppOptions.LastWindowCloseBehavior.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.LastWindowCloseBehavior.Value = value.Value;
            }
        }
    }

    [Command("SetBackgroundImageFolderCommand")]
    private async Task SetBackgroundImageFolderAsync()
    {
        ValueResult<bool, string?> result = FileSystemPickerInteraction.PickFolder(SH.ViewPageSettingBackgroundImagePickFolderTitle);
        if (result.TryGetValue(out string? path))
        {
            await TaskContext.SwitchToMainThreadAsync();
            AppOptions.BackgroundImagePath.Value = path;
        }
    }

    [Command("ResetBackgroundImageFolderCommand")]
    private void ResetBackgroundImageFolder()
    {
        AppOptions.BackgroundImagePath.Value = string.Empty;
    }
}
