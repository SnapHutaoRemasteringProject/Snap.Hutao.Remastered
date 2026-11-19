// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

public interface IWindowedContentDialogQueue
{
    bool IsDialogShowing { get; }

    ValueWindowedContentDialogTask EnqueueAndShowAsync(WinUI3Package.WindowedContentDialog windowedContentDialog);
}
