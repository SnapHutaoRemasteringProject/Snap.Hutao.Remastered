// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using WinRT;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

[DependencyProperty<string>("Speed")]
[DependencyProperty<string>("RemainingTime")]
[DependencyProperty<int>("Value", DefaultValue = 0, PropertyChangedCallbackName = nameof(OnChunksChanged), NotNull = true)]
[DependencyProperty<int>("Maximum", DefaultValue = -1, PropertyChangedCallbackName = nameof(OnChunksChanged), NotNull = true)]
[DependencyProperty<string>("Description")]
[DependencyProperty<string>("IconGlyph")]
public sealed partial class SophonProgressBar : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(SophonProgressBar), new(default));

    public SophonProgressBar()
    {
        InitializeComponent();
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public double ProgressValue { get => Maximum <= 0 ? 0D : Math.Clamp(1D * Value / Maximum, 0D, 1D); }

    public string ProgressPercentFormatted { get => $"{ProgressValue:P2}"; }

    public string ProgressFormatted { get => Maximum > -1 ? $"{Value} / {Maximum}" : SH.UIXamlViewSpecializedSophonProgressDefault; }

    public bool IsIndeterminate { get => Maximum is -1; }

    private static void OnChunksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        SophonProgressBar sender = d.As<SophonProgressBar>();
        sender.OnPropertyChanged(nameof(ProgressValue));
        sender.OnPropertyChanged(nameof(ProgressPercentFormatted));
        sender.OnPropertyChanged(nameof(ProgressFormatted));
        sender.OnPropertyChanged(nameof(IsIndeterminate));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}