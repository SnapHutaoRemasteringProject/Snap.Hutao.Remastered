// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Windowing;
using Snap.Hutao.Core.Graphics;
using Windows.Graphics;

namespace Snap.Hutao.UI.Windowing;

internal static class AppWindowExtension
{
    extension(AppWindow appWindow)
    {
        public unsafe RectInt32 Rect
        {
            get
            {
                RectInt32View view = default;
                view.Position = appWindow.Position;
                view.Size = appWindow.Size;
                return *(RectInt32*)&view;
            }
        }

        public unsafe void MoveThenResize(RectInt32 rectInt32)
        {
            RectInt32View* pView = (RectInt32View*)&rectInt32;
            appWindow.Move(pView->Position);
            appWindow.Resize(pView->Size);
        }

        public void SafeIsShowInSwitchers(bool value)
        {
            try
            {
                // Some users use a custom task bar and which doesn't implement ITaskbarList
                // WinUI use ITaskbarList.AddTab & .DeleteTab to show/hide tab as of now (WASDK 1.7)
                // At Microsoft.UI.Windowing.dll
                appWindow.IsShownInSwitchers = value;
            }
            catch (NotImplementedException)
            {
                // SetShownInSwitchers failed.
            }
        }
    }
}