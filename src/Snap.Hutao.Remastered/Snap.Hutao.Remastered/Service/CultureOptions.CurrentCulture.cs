// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Core.Setting;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service;

public sealed partial class CultureOptions
{
    [field: MaybeNull]
    public IObservableProperty<CultureInfo> CurrentCulture { get => field ??= CreatePropertyForClassUsingCustom(SettingKeys.PrimaryLanguage, SupportedCultures.GetSupportedCulture(CultureInfo.CurrentUICulture), static v => SupportedCultures.GetSupportedCulture(CultureInfo.GetCultureInfo(v)), static v => v.Name); }
}
