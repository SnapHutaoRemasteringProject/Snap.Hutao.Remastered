// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Reflection;

namespace Snap.Hutao.Remastered.Core.AI.Diagnostics;

internal static class SentryEnvironmentVariables
{
    private const string DefaultDsn = "https://87c60195ae5a60f0038387547f7cd034@o4510769861820416.ingest.us.sentry.io/4510844382871552";
    private const string DsnEnvironmentVariable = "SENTRY_DSN";
    private const string EnvironmentEnvironmentVariable = "SENTRY_ENVIRONMENT";
    private const string ReleaseEnvironmentVariable = "SENTRY_RELEASE";
    private const string TracesSampleRateEnvironmentVariable = "SENTRY_TRACES_SAMPLE_RATE";
    private const double DefaultTracesSampleRate = 1.0;

    public static string GetDsn()
    {
        return GetEnvironmentValue(DsnEnvironmentVariable) ?? DefaultDsn;
    }

    public static string GetEnvironmentName()
    {
        if (GetEnvironmentValue(EnvironmentEnvironmentVariable) is { } environment)
        {
            return environment;
        }

#if DEBUG
        return Environments.Development;
#else
        return Environments.Production;
#endif
    }

    public static string GetReleaseName(string applicationName, Assembly assembly)
    {
        if (GetEnvironmentValue(ReleaseEnvironmentVariable) is { } release)
        {
            return release;
        }

        string? informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return $"{applicationName}@{informationalVersion}";
        }

        if (assembly.GetName().Version is { } version)
        {
            return $"{applicationName}@{version}";
        }

        return applicationName;
    }

    public static double GetTracesSampleRate()
    {
        if (GetEnvironmentValue(TracesSampleRateEnvironmentVariable) is not { } value)
        {
            return DefaultTracesSampleRate;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double sampleRate))
        {
            return DefaultTracesSampleRate;
        }

        return sampleRate is >= 0.0 and <= 1.0 ? sampleRate : DefaultTracesSampleRate;
    }

    private static string? GetEnvironmentValue(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
