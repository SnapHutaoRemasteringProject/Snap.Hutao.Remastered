// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Converter;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

// The page hands over the raw DescriptionsParameters and the control builds the
// level list itself. A converter binding from the template would put a closed
// generic IList<LevelParameters<...>> into a dependency property, which the
// native build cannot resolve; the control only exchanges non-generic types.
[DependencyProperty<object>("Proud", PropertyChangedCallbackName = nameof(OnProudChanged))]
[DependencyProperty<System.Collections.IList>("Source")]
[DependencyProperty<ParameterLevel>("SelectedItem")]
[DependencyProperty<int>("PreferredSelectedIndex", DefaultValue = 0, NotNull = true)]
public sealed partial class DescParamComboBox : UserControl
{
    public DescParamComboBox()
    {
        InitializeComponent();
    }

    private static void OnProudChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not DescParamComboBox descParamComboBox)
        {
            return;
        }

        descParamComboBox.Bindings.Update();

        if (args.NewValue is not DescriptionsParameters proud)
        {
            descParamComboBox.Source = null;
            descParamComboBox.SelectedItem = null;
            return;
        }

        try
        {
            IList<ParameterLevel> levels = new DescriptionsParametersDescriptor().Convert(proud);
            descParamComboBox.Source = (System.Collections.IList)levels;
            descParamComboBox.SelectedItem = levels.ElementAtOrDefault(descParamComboBox.PreferredSelectedIndex) ?? levels.LastOrDefault();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            descParamComboBox.Source = null;
            descParamComboBox.SelectedItem = null;
        }
    }
}
