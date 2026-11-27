using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace PrototypeHost;

public partial class App : Application
{
    private MainWindow? window;
    private readonly Dictionary<string, string> providers = new(StringComparer.Ordinal);

    internal static App CurrentHost { get; private set; } = null!;

    public App()
    {
        CurrentHost = this;
        // DoNotGenerateOtherProviders disables automatic provider construction.
        // Restore the platform provider before parsing App.xaml (XamlControlsResources).
        AddOtherProvider(new global::Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider());
        InitializeComponent();
    }

    // The generated WinUI App metadata provider retains the component provider.
    // The platform provider is explicitly registered above; resource resolution is unchanged.
    internal void RegisterMetadataProvider(string path, string name, IXamlMetadataProvider provider)
    {
        if (providers.TryGetValue(name, out string? existing))
        {
            if (!string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("XAML metadata name collision: " + name);
            return;
        }
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
