// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Snap.Hutao.UI.Xaml.Markup;

/// <summary>
/// Xaml 服务提供器扩展
/// </summary>
internal static class XamlServiceProviderExtension
{
    extension(IXamlServiceProvider provider)
    {
        /// <summary>
        /// Get IProvideValueTarget from serviceProvider
        /// </summary>
        /// <returns>IProvideValueTarget</returns>
        public IProvideValueTarget GetProvideValueTarget()
        {
            return (IProvideValueTarget)provider.GetService(typeof(IProvideValueTarget));
        }

        /// <summary>
        /// Get IRootObjectProvider from serviceProvider
        /// </summary>
        /// <returns>IRootObjectProvider</returns>
        public IRootObjectProvider GetRootObjectProvider()
        {
            return (IRootObjectProvider)provider.GetService(typeof(IRootObjectProvider));
        }

        /// <summary>
        /// Get IUriContext from serviceProvider
        /// </summary>
        /// <returns>IUriContext</returns>
        public IUriContext GetUriContext()
        {
            return (IUriContext)provider.GetService(typeof(IUriContext));
        }

        /// <summary>
        /// Get IXamlTypeResolver from serviceProvider
        /// </summary>
        /// <returns>IXamlTypeResolver</returns>
        public IXamlTypeResolver GetXamlTypeResolver()
        {
            return (IXamlTypeResolver)provider.GetService(typeof(IXamlTypeResolver));
        }
    }
}