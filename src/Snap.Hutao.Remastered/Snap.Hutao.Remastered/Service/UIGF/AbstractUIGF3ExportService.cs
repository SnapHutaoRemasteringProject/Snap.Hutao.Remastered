// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Model.Metadata.Abstraction;
using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Remastered.Service.UIGF;

public abstract partial class AbstractUIGF3ExportService : IUIGFExportService
{
    protected readonly JsonSerializerOptions jsonOptions;
    protected readonly IServiceProvider serviceProvider;
    protected readonly ITaskContext taskContext;
    protected readonly IMetadataService metadataService;

    [GeneratedConstructor]
    public partial AbstractUIGF3ExportService(IServiceProvider serviceProvider);

    protected abstract string Version { get; }

    public virtual async ValueTask ExportAsync(UIGFExportOptions exportOptions, CancellationToken token = default)
    {
        await taskContext.SwitchToBackgroundAsync();

        GachaLogServiceMetadataContext metadataContext = await metadataService.GetContextAsync<GachaLogServiceMetadataContext>(token).ConfigureAwait(false);

        // Legacy format only supports single UID
        uint uid = exportOptions.GachaArchiveUids.Length > 0 ? exportOptions.GachaArchiveUids[0] : 0;

        Model.InterChange.GachaLog.UIGF3 uigf = new()
        {
            Info = new()
            {
                Uid = uid,
                ExportApp = "Snap Hutao Remastered",
                ExportAppVersion = $"{HutaoRuntime.Version}",
                ExportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                ExportTime = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UigfVersion = Version,
                RegionTimeZone = InferRegionTimeZone(uid),
            },
        };

        ExportGachaItems(uigf, uid, metadataContext);

        using (FileStream stream = File.Create(exportOptions.FilePath))
        {
            await JsonSerialize.SerializeAsync(stream, uigf, jsonOptions, token).ConfigureAwait(false);
        }
    }

    protected virtual void ExportGachaItems(Model.InterChange.GachaLog.UIGF3 uigf, uint uid, GachaLogServiceMetadataContext metadataContext)
    {
        if (uid == 0)
        {
            return;
        }

        IGachaLogRepository gachaLogRepository = serviceProvider.GetRequiredService<IGachaLogRepository>();

        GachaArchive? archive = gachaLogRepository.GetGachaArchiveByUid($"{uid}");
        ArgumentNullException.ThrowIfNull(archive);

        ImmutableArray<GachaItem> dbItems = gachaLogRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId);

        uigf.List = dbItems.SelectAsArray(item =>
        {
            INameQualityAccess nameQuality = metadataContext.GetNameQualityByItemId(item.ItemId);
            string itemType = item.ItemId.StringLength switch
            {
                8U => SH.ModelInterchangeUIGFItemTypeAvatar,
                5U => SH.ModelInterchangeUIGFItemTypeWeapon,
                _ => string.Empty,
            };
            return Hk4eItem.From(item, nameQuality.Name, itemType, ((int)nameQuality.Quality).ToString(), InferRegionTimeZone(uid));
        });
    }

    protected static int InferRegionTimeZone(uint uid)
    {
        char first = $"{uid}"[0];
        return first switch
        {
            '6' => -5,
            '7' => 1,
            _ => 8,
        };
    }
}
