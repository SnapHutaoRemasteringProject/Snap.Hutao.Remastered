// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Web.Bridge;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
internal sealed partial class SettingWebViewViewModel : Abstraction.ViewModel
{
    [GeneratedConstructor]
    public partial SettingWebViewViewModel(IServiceProvider serviceProvider);

    public partial AppOptions AppOptions { get; }

    // TODO: Replace with IObservableProperty
    public NameValue<BridgeShareSaveType>? SelectedShareSaveType
    {
        get => field ??= AppOptions.BridgeShareSaveTypes.Single(t => t.Value == AppOptions.BridgeShareSaveType.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.BridgeShareSaveType.Value = value.Value;
            }
        }
    }
}