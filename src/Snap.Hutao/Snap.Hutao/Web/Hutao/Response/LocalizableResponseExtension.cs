// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Globalization;

namespace Snap.Hutao.Web.Hutao.Response;

internal static class LocalizableResponseExtension
{
    extension(ILocalizableResponse localizableResponse)
    {
        public string? GetLocalizationMessageOrDefault()
        {
            string? key = localizableResponse.LocalizationKey;
            return string.IsNullOrEmpty(key) ? default : SH.ResourceManager.GetString(key, CultureInfo.CurrentCulture);
        }

        public string GetLocalizationMessage()
        {
            string? key = localizableResponse.LocalizationKey;
            return string.IsNullOrEmpty(key) ? string.Empty : SH.ResourceManager.GetString(key, CultureInfo.CurrentCulture) ?? string.Empty;
        }
    }

    extension<TResponse>(TResponse localizableResponse)
        where TResponse : Web.Response.Response, ILocalizableResponse
    {
        public string GetLocalizationMessageOrMessage()
        {
            return localizableResponse.GetLocalizationMessageOrDefault() ?? localizableResponse.Message;
        }
    }
}