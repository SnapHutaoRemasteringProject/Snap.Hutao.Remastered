// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using System.IO;

namespace Snap.Hutao.Remastered.Core.Shell;

[Service(ServiceLifetime.Transient, typeof(IShellLinkInterop))]
public sealed class ShellLinkInterop : IShellLinkInterop
{
    public bool TryCreateDesktopShortcut()
    {
        string targetLogoPath = HutaoRuntime.GetDataDirectoryFile("ShellLinkLogo.ico");

        try
        {
            InstalledLocation.CopyFileFromApplicationUri("ms-appx:///Assets/Logo.ico", targetLogoPath);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string target = Path.Combine(desktop, $"{SH.AppName}.lnk");

            FileSystem.CreateLink("shell:appsFolder\\E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8_k3erpsn8bwzzy!App", "", targetLogoPath, target);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryCreateGameLaunchShortcut()
    {
        string targetLogoPath = HutaoRuntime.GetDataDirectoryFile("ShellLinkLogo.ico");

        try
        {
            InstalledLocation.CopyFileFromApplicationUri("ms-appx:///Assets/Logo.ico", targetLogoPath);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutFileName = $"{SH.AppName} - {SH.ViewPageLaunchGameAction}.lnk";
            string target = Path.Combine(desktop, shortcutFileName);

            // Try creating a .lnk via the native COM CreateLink with hutao://launch URI as target
            try
            {
                FileSystem.CreateLink("hutao://launch", "", targetLogoPath, target);
                return true;
            }
            catch
            {
                // Fallback: create a .url file (Internet Shortcut) which natively supports protocol URIs
                string urlTarget = target.Replace(".lnk", ".url");
                string urlContent = $$"""
                    [InternetShortcut]
                    URL=hutao://launch
                    IconFile={{targetLogoPath}}
                    IconIndex=0
                    """;
                File.WriteAllText(urlTarget, urlContent);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}