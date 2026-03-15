// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginPage : ScopedPage
{
    public PluginPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginViewModel>();
    }
}

public partial class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
        {
            bool isNullOrEmpty = string.IsNullOrEmpty(str);
            bool invert = parameter as string == "Inverted";
            
            if (invert)
            {
                return isNullOrEmpty ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return isNullOrEmpty ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            }
        }
        
        return Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is System.Collections.IEnumerable enumerable)
        {
            List<string> items = new List<string>();
            foreach (object? item in enumerable)
            {
                items.Add(item?.ToString() ?? string.Empty);
            }
            return string.Join(", ", items.Where(s => !string.IsNullOrEmpty(s)));
        }
        
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public partial class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            // 使用 SH 类获取本地化字符串
            return boolValue ? 
                SH.ResourceManager.GetString("ViewPagePluginStatusEnabled", System.Globalization.CultureInfo.CurrentCulture) ?? "已启用" : 
                SH.ResourceManager.GetString("ViewPagePluginStatusDisabled", System.Globalization.CultureInfo.CurrentCulture) ?? "已禁用";
        }
        
        return SH.ResourceManager.GetString("ViewPagePluginStatusDisabled", System.Globalization.CultureInfo.CurrentCulture) ?? "已禁用";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
