// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.UIGF;

public interface IUIGFService
{
    ValueTask ExportAsync(UIGFExportOptions exportOptions, CancellationToken token = default);

    ValueTask ImportAsync(UIGFImportOptions importOptions, CancellationToken token = default);

    bool Parse(string json, out Model.InterChange.GachaLog.UIGF? uigf);
}