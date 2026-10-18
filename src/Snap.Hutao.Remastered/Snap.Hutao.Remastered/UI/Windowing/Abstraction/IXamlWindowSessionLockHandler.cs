// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Windowing.Abstraction;

public interface IXamlWindowSessionLockHandler
{
    void OnSessionLockChanged(bool isLocked);
}
