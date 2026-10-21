// Replace only app-specific paths, Windows credentials, and telemetry.
// File access, JSON, observation, retries, and file watching use production code.
namespace Snap.Hutao.Remastered.Core.AI.IO
{
    internal static class WellKnownLocations { public static string Settings { get; set; } = string.Empty; }
}
namespace Snap.Hutao.Remastered.Service.AI.Settings
{
    internal static class AgentCredentialStore
    {
        public static int SaveCalls;
        public static void Load(AgentSettings settings) { }
        public static void Save(AgentSettings settings) => Interlocked.Increment(ref SaveCalls);
    }
}
namespace Sentry { internal enum SpanStatus { Cancelled, FailedPrecondition } }
namespace Snap.Hutao.Remastered.Core.AI.Diagnostics
{
    internal sealed class SentryDiagnosticSpan : IDisposable
    {
        public void SetTag(string name, object value) { }
        public void SetData(string name, object value) { }
        public void Finish(Sentry.SpanStatus status) { }
        public void Dispose() { }
    }
    internal static class SentryDiagnostics
    {
        public static SentryDiagnosticSpan StartSpan(string operation, string description) => new();
        public static void AddBreadcrumb(string message, string category, string type) { }
        public static void CaptureException(Exception exception, SentryDiagnosticSpan span, string operation) { }
    }
}
