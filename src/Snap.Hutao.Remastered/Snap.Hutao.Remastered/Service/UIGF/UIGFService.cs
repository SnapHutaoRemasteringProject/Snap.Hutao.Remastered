// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Service.Notification;

namespace Snap.Hutao.Remastered.Service.UIGF;

[Service(ServiceLifetime.Singleton, typeof(IUIGFService))]
public sealed partial class UIGFService : IUIGFService
{
    private readonly IServiceProvider serviceProvider;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly IMessenger messenger;

    [GeneratedConstructor]
    public partial UIGFService(IServiceProvider serviceProvider);

    public ValueTask ExportAsync(UIGFExportOptions exportOptions, CancellationToken token = default)
    {
        IUIGFExportService exportService = serviceProvider.GetRequiredKeyedService<IUIGFExportService>(exportOptions.Version);
        return exportService.ExportAsync(exportOptions, token);
    }

    public ValueTask ImportAsync(UIGFImportOptions importOptions, CancellationToken token = default)
    {
        UIGFVersion version = importOptions.UIGF.Info.Version switch
        {
            "v2.0" or "v2.1" or "v2.2" or "v2.3" or "v2.4" or "v3.0" => UIGFVersion.UIGF30,
            "v4.0" => UIGFVersion.UIGF40,
            "v4.1" => UIGFVersion.UIGF41,
            "v4.2" => UIGFVersion.UIGF42,
            _ => UIGFVersion.None,
        };

        IUIGFImportService importService = serviceProvider.GetRequiredKeyedService<IUIGFImportService>(version);
        return importService.ImportAsync(importOptions, token);
    }

    public bool Parse(string json, out UIGF4? uigf)
    {
        uigf = null;
        UIGFView? view = null;
        try
        {
            view = UIGFView.Create(json);
            if (view.Version is not null)
            {
                if (view.IsLegacy)
                {
                    UIGF3? legacy = JsonSerialize.Deserialize<UIGF3>(json, jsonOptions);
                    if (legacy is not null)
                    {
                        uigf = ConvertFromLegacy(legacy);
                    }
                }
                else
                {
                    uigf = JsonSerialize.Deserialize<UIGF42>(json, jsonOptions);
                }
            }
        }
        catch(Exception ex)
        {
            messenger.Send(InfoBarMessage.Error($"uigf version: {view?.Version}", ex));
        }
        return uigf is not null;
    }

    private static UIGF4 ConvertFromLegacy(UIGF3 legacy)
    {
        UIGF4 result = new()
        {
            Info = new()
            {
                ExportApp = legacy.Info.ExportApp ?? string.Empty,
                ExportAppVersion = legacy.Info.ExportAppVersion ?? string.Empty,
                ExportTimestamp = legacy.Info.ExportTimestamp,
                Version = legacy.Info.UigfVersion,
            },
        };

        if (!legacy.List.IsDefaultOrEmpty)
        {
            UIGFEntry<Hk4eItem> entry = new()
            {
                Uid = legacy.Info.Uid,
                TimeZone = legacy.Info.RegionTimeZone,
                Language = legacy.Info.Lang,
                List = legacy.List,
            };

            result.Hk4e = [entry];
        }

        return result;
    }
}
