// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml;

// Use WinUI's native DataContext property rather than registering a second
// object-valued property on a managed DependencyObject for every proxy instance.
// All resource proxies keep the same DataContext binding path and notifications.
public sealed partial class BindingProxy : FrameworkElement;
