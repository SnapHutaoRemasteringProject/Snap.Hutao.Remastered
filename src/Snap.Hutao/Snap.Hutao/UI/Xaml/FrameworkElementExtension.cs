// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.UI.Xaml;

internal static class FrameworkElementExtension
{
    extension(FrameworkElement frameworkElement)
    {
        /// <summary>
        /// Make properties below false:
        /// <code>
        /// * AllowFocusOnInteraction
        /// * IsDoubleTapEnabled
        /// * IsHitTestVisible
        /// * IsHoldingEnabled
        /// * IsRightTapEnabled
        /// * IsTabStop
        /// </code>
        /// </summary>
        public void DisableInteraction()
        {
            frameworkElement.AllowFocusOnInteraction = false;
            frameworkElement.IsDoubleTapEnabled = false;
            frameworkElement.IsHitTestVisible = false;
            frameworkElement.IsHoldingEnabled = false;
            frameworkElement.IsRightTapEnabled = false;
            frameworkElement.IsTabStop = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? DataContext<T>()
            where T : class
        {
            return frameworkElement.DataContext as T;
        }

        public void InitializeDataContext<TDataContext>(IServiceProvider serviceProvider)
            where TDataContext : class
        {
            try
            {
                frameworkElement.DataContext = serviceProvider.GetRequiredService<TDataContext>();
                (frameworkElement as IDataContextInitialized)?.OnDataContextInitialized(serviceProvider);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }
    }
}