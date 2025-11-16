// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Snap.Hutao.Extension;

internal static class AppNotificationBuilderExtension
{
    extension(AppNotificationBuilder builder)
    {
        /// <summary>
        /// Build and show the notification
        /// </summary>
        /// <param name="manager">Defaults to <see cref="AppNotificationManager.Default"/></param>
        public void Show(AppNotificationManager? manager = default)
        {
            (manager ?? AppNotificationManager.Default).Show(builder.BuildNotification());
        }
    }
}