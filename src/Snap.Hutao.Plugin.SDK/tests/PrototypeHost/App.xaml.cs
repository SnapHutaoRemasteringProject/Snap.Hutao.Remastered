using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace PrototypeHost;

public partial class App : Application
{
    private MainWindow? window;
    private PluginPriResources? pluginResources;
    private readonly Dictionary<string, string> providers = new(StringComparer.Ordinal);

    internal static App CurrentHost { get; private set; } = null!;
    internal string? LastResolvedPluginResource => pluginResources?.LastResolvedResource;

    public App()
    {
        CurrentHost = this;
        // WinUI asks before its first resource lookup (including App.xaml).
        ResourceManagerRequested += (_, args) =>
        {
            pluginResources ??= new PluginPriResources();
            args.CustomResourceManager = pluginResources.Manager;
        };
        // DoNotGenerateOtherProviders disables automatic provider construction.
        // Restore the platform provider before parsing App.xaml (XamlControlsResources).
        AddOtherProvider(new global::Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider());
        InitializeComponent();
    }

    // The generated WinUI App metadata provider retains the component provider.
    // The matching component PRI is registered before any plugin page is initialized.
    internal void RegisterMetadataProvider(string path, string name, IXamlMetadataProvider provider)
    {
        if (providers.TryGetValue(name, out string? existing))
        {
            if (!string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("XAML metadata name collision: " + name);
            return;
        }
        (pluginResources ?? throw new InvalidOperationException("WinUI did not request its MRT resource manager.")).Register(path);
        AddOtherProvider(provider);
        providers.Add(name, path);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            window = new MainWindow();
            window.Activate();
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            try
            {
                Directory.CreateDirectory(MainPage.TestRoot);
                File.WriteAllText(Path.Combine(MainPage.TestRoot, "startup-error.txt"), error.ToString());
            }
            catch (Exception logError) { System.Diagnostics.Debug.WriteLine(logError); }
            throw;
        }
    }
}
