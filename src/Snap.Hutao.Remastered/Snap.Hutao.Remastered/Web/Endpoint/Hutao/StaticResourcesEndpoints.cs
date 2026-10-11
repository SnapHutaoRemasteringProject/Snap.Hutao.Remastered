// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

/// <remarks>
/// Statically reachable on purpose: XAML value converters (which build icon urls from their static field
/// initializers and <c>Convert</c> methods) are instantiated by the XAML parser and cannot take injected
/// services. The domain itself is owned by <see cref="IServerDomainService"/>.
/// </remarks>
public static class StaticResourcesEndpoints
{
    public static string Root { get => ServerDomainService.Current.GetApiRoot(); }

    public static Uri UIIconNone { get => StaticRaw("Bg", "UI_Icon_None.png").ToUri(); }

    public static Uri UIItemIconNone { get => StaticRaw("Bg", "UI_ItemIcon_None.png").ToUri(); }

    public static Uri UIAvatarIconSideNone { get => StaticRaw("AvatarIcon", "UI_AvatarIcon_Side_None.png").ToUri(); }

    public static string StaticRaw(string category, string fileName)
    {
        return string.Intern($"{Root}/static/raw/{category}/{fileName}");
    }

    public static string StaticZip(string fileName)
    {
        return $"{Root}/static/zip/{fileName}.zip";
    }

    public static string StaticSize()
    {
        return $"{Root}/static/size";
    }
}