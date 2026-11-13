// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Snap.Hutao.Remastered.Web.Request.Builder;

internal static class HttpRequestFailureDetails
{
    public static string Format(Exception exception, Type responseType, Uri? requestUri, HttpResponseMessage? response, string stage)
    {
        StringBuilder text = new();
        text.AppendLine($"UTC: {DateTime.UtcNow:O}; PID: {Environment.ProcessId}; Stage: {stage}");
        text.AppendLine($"Build: {typeof(HttpRequestFailureDetails).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        text.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}; DynamicCode: {RuntimeFeature.IsDynamicCodeCompiled}");
        text.AppendLine($"Executable: {Environment.ProcessPath}");
        text.AppendLine($"ResponseType: {responseType.FullName}");
        string? endpoint = requestUri is { IsAbsoluteUri: true }
            ? requestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped)
            : null;
        text.AppendLine($"Endpoint: {endpoint}; HTTP: {(int?)response?.StatusCode}; ContentType: {response?.Content.Headers.ContentType?.MediaType}");

        // Account requests contain credentials. Keep structural diagnostics, not
        // response bodies, headers, query strings or arbitrary exception messages.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            text.AppendLine($"Exception: {current.GetType().FullName}; HRESULT: {current.HResult:X8}");
            if (current is JsonException json)
            {
                text.AppendLine($"JSON path: {json.Path}; Line: {json.LineNumber}; Byte: {json.BytePositionInLine}");
            }

            text.AppendLine(current.StackTrace);
        }

        return text.ToString();
    }
}
