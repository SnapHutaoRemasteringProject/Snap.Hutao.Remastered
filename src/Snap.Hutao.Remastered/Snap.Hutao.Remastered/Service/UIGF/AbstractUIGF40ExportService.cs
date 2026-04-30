// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Service.GachaLog;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Remastered.Service.UIGF;

public abstract partial class AbstractUIGF40ExportService : IUIGFExportService
{
    protected readonly JsonSerializerOptions jsonOptions;
    protected readonly IServiceProvider serviceProvider;
    protected readonly ITaskContext taskContext;

    [GeneratedConstructor]
    public partial AbstractUIGF40ExportService(IServiceProvider serviceProvider);

    protected abstract string Version { get; }

    public virtual async ValueTask ExportAsync(UIGFExportOptions exportOptions, CancellationToken token = default)
    {
        await taskContext.SwitchToBackgroundAsync();

        Model.InterChange.GachaLog.UIGF uigf = new()
        {
            Info = new()
            {
                ExportApp = "Snap Hutao Remastered",
                ExportAppVersion = $"{HutaoRuntime.Version}",
                ExportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Version = Version,
            },
        };

        ExportGachaArchives(uigf, exportOptions.GachaArchiveUids);

        using (FileStream stream = File.Create(exportOptions.FilePath))
        {
            await JsonSerializer.SerializeAsync(stream, uigf, jsonOptions, token).ConfigureAwait(false);
        }
    }

    protected virtual void ExportGachaArchives(Model.InterChange.GachaLog.UIGF uigf, ImmutableArray<uint> uids)
    {
        if (uids.Length <= 0)
        {
            return;
        }

        IGachaLogRepository gachaLogRepository = serviceProvider.GetRequiredService<IGachaLogRepository>();

        ImmutableArray<UIGFEntry<Hk4eItem>>.Builder hk4eResults = ImmutableArray.CreateBuilder<UIGFEntry<Hk4eItem>>(uids.Length);

        foreach (ref readonly uint uid in uids.AsSpan())
        {
            GachaArchive? archive = gachaLogRepository.GetGachaArchiveByUid($"{uid}");
            ArgumentNullException.ThrowIfNull(archive);
            
            // Export standard gacha items
            ImmutableArray<GachaItem> dbItems = gachaLogRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId);
            int timezone = 0;
            if (dbItems.Length > 0)
            {
                timezone = (int)dbItems[0].Time.Offset.Hours;
            }

            UIGFEntry<Hk4eItem> hk4eEntry = new()
            {
                Uid = uid,
                TimeZone = timezone,
                List = dbItems.SelectAsArray(Hk4eItem.From),
            };
            hk4eResults.Add(hk4eEntry);
        }

        uigf.Hk4e = hk4eResults.ToImmutable();
    }
}
