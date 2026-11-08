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
        public static Action? BeforeLoad;
        public static Action? BeforeSave;
        public static void Load(AgentSettings settings) { BeforeLoad?.Invoke(); }
        public static void Save(AgentSettings settings)
        {
            Interlocked.Increment(ref SaveCalls);
            BeforeSave?.Invoke();
        }
    }
}
namespace Sentry
{
    // The linked settings provider emits breadcrumbs through BreadcrumbFactory.
    public enum BreadcrumbLevel { Debug, Info, Warning, Error, Fatal }

    public sealed class Breadcrumb
    {
        public Breadcrumb(string message, string type, IReadOnlyDictionary<string, string>? data = null, string? category = null, BreadcrumbLevel level = default)
        {
            Message = message;
            Type = type;
            Data = data;
            Category = category;
            Level = level;
        }

        public string Message { get; }
        public string Type { get; }
        public IReadOnlyDictionary<string, string>? Data { get; }
        public string? Category { get; }
        public BreadcrumbLevel Level { get; }
    }
}
