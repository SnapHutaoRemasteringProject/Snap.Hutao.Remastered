// The lifecycle and observable properties are production code. Only OS process
// discovery, native error display, property factory and WinUI weak-listener helper
// are substituted. Event forwarding in the read-only wrapper remains production code.
global using System.ComponentModel;

using Snap.Hutao.Remastered.Core.Diagnostics;

namespace Snap.Hutao.Remastered.Extension
{
    internal static class GamePropertyTestExtensions
    {
        public static void WeakPropertyChanged<T>(this INotifyPropertyChanged source, T target, Action<T, object?, PropertyChangedEventArgs> callback)
            where T : class
        {
            WeakReference<T> reference = new(target);
            PropertyChangedEventHandler? handler = null;
            handler = (sender, args) =>
            {
                if (reference.TryGetTarget(out T? instance)) { callback(instance, sender, args); }
                else { source.PropertyChanged -= handler; }
            };
            source.PropertyChanged += handler;
        }
    }
}

namespace JetBrains.Annotations
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class RequireStaticDelegateAttribute : Attribute;
}

namespace Snap.Hutao.Remastered.Core.Property
{
    internal static class Property
    {
        public static IObservableProperty<T> CreateObservable<T>(T value) => new ObservableProperty<T>(value);
    }
}

namespace Snap.Hutao.Remastered.Core.Diagnostics
{
    public interface IProcess : IDisposable
    {
        bool HasExited { get; }
        void Kill();
        Task WaitForExitAsync(CancellationToken token = default);
    }
}

namespace Snap.Hutao.Remastered.Factory.Process
{
    internal static class ProcessFactory
    {
        public static IProcess? Current { get; set; }
        public static List<IProcess> Additional { get; } = [];
        public static int DiscoveryCount { get; set; }
        public static List<DiagnosticsProcess> Handles { get; } = [];

        public static bool IsRunning(ReadOnlySpan<string> names, out IProcess? process)
        {
            List<DiagnosticsProcess> matches = GetRunningProcesses(names);
            process = matches.FirstOrDefault();
            foreach (DiagnosticsProcess other in matches.Skip(1)) { other.Dispose(); }
            return process is not null;
        }

        public static List<DiagnosticsProcess> GetRunningProcesses(ReadOnlySpan<string> names)
        {
            DiscoveryCount++;
            List<IProcess> candidates = [.. Additional];
            if (Current is not null) { candidates.Add(Current); }
            List<DiagnosticsProcess> matches = candidates.Where(static process => !process.HasExited).Select(static process => new DiagnosticsProcess(process)).ToList();
            Handles.AddRange(matches);
            return matches;
        }
    }

    internal sealed class DiagnosticsProcess(IProcess process) : IProcess
    {
        public int Id => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(process);
        public bool IsDisposed { get; private set; }
        public bool HasExited => process.HasExited;
        public void Kill() => process.Kill();
        public Task WaitForExitAsync(CancellationToken token = default) => process.WaitForExitAsync(token);
        public Task KillAndWaitForExitAsync(CancellationToken token)
        {
            process.Kill();
            return process.WaitForExitAsync(token);
        }
        public void Dispose() => IsDisposed = true;
    }
}

namespace Snap.Hutao.Remastered.Win32
{
    internal sealed class HutaoNative
    {
        public static HutaoNative Instance { get; } = new();
        public static List<string> Errors { get; } = [];
        public void ShowErrorMessage(string title, string message) => Errors.Add(message);
    }
}

namespace Snap.Hutao.Remastered.Win32.Foundation
{
    internal enum WIN32_ERROR { ERROR_ACCESS_DENIED = 5 }
}

namespace Snap.Hutao.Remastered.Service.Game
{
    internal static class SH
    {
        public const string ServiceGameLaunchExecutionGameRunningKillFailed = "Game process could not be terminated";
        public const string ServiceGameStopProcessAccessDenied = "Run Snap Hutao as administrator";
        public const string ServiceGameLaunchPhaseWaitingProcessExit = "Waiting for the game process to exit";
    }
}
