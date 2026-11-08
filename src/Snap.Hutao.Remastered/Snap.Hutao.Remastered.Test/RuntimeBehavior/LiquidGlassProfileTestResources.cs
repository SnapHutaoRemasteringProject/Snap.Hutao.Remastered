// Execute the production profile and queued callback. Only the Windows-bound
// brush, dispatcher, settings and telemetry endpoints are replaced here.
global using CommunityToolkit.Mvvm.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection;
global using SentrySdk = Snap.Hutao.Remastered.Test.RuntimeBehavior.LiquidGlassTestTelemetry;

namespace WinUI.Composition.Hlsl
{
    public sealed class LiquidGlassBrush
    {
        public double SpecularWidth { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
    }
}

namespace Snap.Hutao.Remastered.Core.Setting
{
    public static class LocalSetting
    {
        public static Dictionary<string, object> Values { get; } = [];
        public static Action<string, object>? BeforeWrite { get; set; }
        public static T Get<T>(string key, T defaultValue = default!) =>
            Values.TryGetValue(key, out object? value) ? (T)value : defaultValue;
        public static void Set<T>(string key, T value)
        {
            BeforeWrite?.Invoke(key, value!);
            Values[key] = value!;
        }
    }
}

namespace Snap.Hutao.Remastered.Service
{
    public sealed class AppOptions
    {
        public TestMaterialOption ControlMaterial { get; } = new();
    }

    public sealed class TestMaterialOption
    {
        public UI.Xaml.Control.Theme.ControlMaterial Value { get; set; }
    }
}

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme
{
    using WinUI.Composition.Hlsl;

    public sealed record LiquidGlassDescriptor(string Key, string EnglishLabel, double Minimum, double Maximum, double Step,
        Func<LiquidGlassBrush, double> Read, Action<LiquidGlassBrush, double> Write);

    public static class LiquidGlassOptics
    {
        public static Action<LiquidGlassBrush, double>? BeforeWrite { get; set; }
        public static readonly LiquidGlassDescriptor[] Descriptors =
        [
            new("SpecularWidth", "Specular width", 0.25, 32, 0.05, static b => b.SpecularWidth, static (b, v) =>
            {
                BeforeWrite?.Invoke(b, v);
                b.SpecularWidth = v;
            }),
        ];
        public static LiquidGlassBrush Defaults(string kind) => new();
        public static double[] Capture(LiquidGlassBrush brush) => [brush.SpecularWidth];
        public static string Text(string key, string fallback) => fallback;
    }

    public sealed class LiquidGlassTestDispatcher
    {
        private readonly Queue<Action> callbacks = new();
        public bool AcceptWork { get; set; } = true;
        public bool TryEnqueue(Action callback)
        {
            if (!AcceptWork) return false;
            callbacks.Enqueue(callback);
            return true;
        }
        public void Drain()
        {
            while (callbacks.TryDequeue(out Action? callback)) callback();
        }
    }

    public sealed class MaterialBindingSource
    {
        public static MaterialBindingSource Current { get; set; } = new();
        public LiquidGlassTestDispatcher DispatcherQueue { get; } = new();
        public int Value => 1;
        public bool GetTypeEnabled(string key) => true;
        public void SetTypeEnabled(string key, bool enabled) { }
    }

    public static class ControlMaterialState
    {
        public static Action? Refresh { get; set; }
        public static void RefreshManagedOptics() => Refresh?.Invoke();
        public static void RefreshManagedInteractions() { }
    }

    public static class XamlApplicationLifetime
    {
        public static bool Exiting { get; set; }
    }

    public enum ControlMaterial { Default, LiquidGlass }

    public static class MaterialTheme
    {
        public static void Apply(ControlMaterial material) { }
    }
}

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior
{
    public static class LiquidGlassTestTelemetry
    {
        public static List<Exception> Exceptions { get; } = [];
        public static bool ThrowOnCapture { get; set; }
        public static void CaptureException(Exception exception)
        {
            CaptureException(exception, static _ => { });
        }
        public static void CaptureException(Exception exception, Action<LiquidGlassTestScope> configure)
        {
            if (ThrowOnCapture) throw new InvalidOperationException("Telemetry unavailable");
            configure(new());
            Exceptions.Add(exception);
        }

        public static void AddBreadcrumb(Sentry.Breadcrumb breadcrumb) { }
    }

    public sealed class LiquidGlassTestScope
    {
        public void SetExtra(string key, string value) { }
    }
}
