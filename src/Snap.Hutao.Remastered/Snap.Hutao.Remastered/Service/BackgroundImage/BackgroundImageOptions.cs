// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Web.Hutao.Wallpaper;

namespace Snap.Hutao.Remastered.Service.BackgroundImage;

[Service(ServiceLifetime.Singleton)]
internal sealed partial class BackgroundImageOptions : ObservableObject
{
    [ObservableProperty]
    public partial Wallpaper? Wallpaper { get; set; }
}