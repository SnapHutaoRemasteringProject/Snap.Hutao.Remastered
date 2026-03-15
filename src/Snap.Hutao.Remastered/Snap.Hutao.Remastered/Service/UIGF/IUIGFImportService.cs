// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.UIGF;

public interface IUIGFImportService
{
    ValueTask ImportAsync(UIGFImportOptions importOptions, CancellationToken token);

    bool Parse(string json, out Model.InterChange.GachaLog.UIGF? uigf);
}