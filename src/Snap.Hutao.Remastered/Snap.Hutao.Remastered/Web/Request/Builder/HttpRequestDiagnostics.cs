// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using System.IO;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Request.Builder;

internal static class HttpRequestDiagnostics
{
    private static readonly Lock Gate = new();

    public static void Write(Exception exception, Type responseType, Uri? requestUri, HttpResponseMessage? response, string stage)
    {
        try
        {
            string text = HttpRequestFailureDetails.Format(exception, responseType, requestUri, response, stage);

            string directory = Path.Combine(AppDataPaths.UnpackagedRoot, "Diagnostics");
            using (Gate.EnterScope())
            {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"http-failures-{Environment.ProcessId}.log");
                if (File.Exists(path) && new FileInfo(path).Length > 262144)
                {
                    File.WriteAllText(path, text);
                }
                else
                {
                    File.AppendAllText(path, text);
                }
            }
        }
        catch (Exception diagnosticError)
        {
            // Diagnostics must not replace the original request exception.
            System.Diagnostics.Trace.WriteLine($"HTTP diagnostics failed: {diagnosticError.GetType().FullName}");
        }
    }
}
