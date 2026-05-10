// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Service.GachaLog;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Remastered.Service.UIGF;

[Service(ServiceLifetime.Transient, typeof(IUIGFExportService), Key = UIGFVersion.UIGF42)]
public sealed partial class UIGF42ExportService : AbstractUIGF40ExportService
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial UIGF42ExportService(IServiceProvider serviceProvider);

    protected override string Version { get; } = "v4.2";

    public override async ValueTask ExportAsync(UIGFExportOptions exportOptions, CancellationToken token = default)
    {
        await taskContext.SwitchToBackgroundAsync();

        Model.InterChange.GachaLog.UIGF42 uigf = new()
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

    private void ExportGachaArchives(Model.InterChange.GachaLog.UIGF42 uigf, ImmutableArray<uint> uids)
    {
        base.ExportGachaArchives(uigf, uids);
        if (uids.Length <= 0)
        {
            return;
        }

        IGachaLogRepository gachaLogRepository = serviceProvider.GetRequiredService<IGachaLogRepository>();

        ImmutableArray<UIGFEntry<Hk4eUGCItem>>.Builder hk4eUgcResults = ImmutableArray.CreateBuilder<UIGFEntry<Hk4eUGCItem>>(uids.Length);

        foreach (ref readonly uint uid in uids.AsSpan())
        {
            GachaArchive? archive = gachaLogRepository.GetGachaArchiveByUid($"{uid}");
            ArgumentNullException.ThrowIfNull(archive);

            // Export beyond gacha (UGC) items
            ImmutableArray<BeyondGachaItem> beyondDbItems = gachaLogRepository.GetBeyondGachaItemImmutableArrayByArchiveId(archive.InnerId);
            if (beyondDbItems.Length > 0)
            {
                UIGFEntry<Hk4eUGCItem> hk4eUgcEntry = new()
                {
                    Uid = uid,
                    TimeZone = 0,
                    List = beyondDbItems.SelectAsArray(item => item.ToHk4eUGCItem()),
                };
                hk4eUgcResults.Add(hk4eUgcEntry);
            }
        }

        uigf.Hk4eUgc = hk4eUgcResults.ToImmutable();
    }
}
