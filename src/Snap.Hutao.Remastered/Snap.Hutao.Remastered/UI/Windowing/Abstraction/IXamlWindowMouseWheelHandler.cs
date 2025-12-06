// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Input;

namespace Snap.Hutao.Remastered.UI.Windowing.Abstraction;

internal interface IXamlWindowMouseWheelHandler
{
    void OnMouseWheel(ref readonly PointerPointProperties data);
}