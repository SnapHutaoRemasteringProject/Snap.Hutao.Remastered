using Snap.Hutao.Remastered.API;
using Snap.Hutao.Remastered.API.Model;
using TestPluginSDK.ViewModel;

namespace TestPluginSDK;

public sealed partial class TestPluginPage : PluginScopedPage
{
    public TestPluginPage(HutaoPlugin plugin) : base(plugin)
    {
        LoadXamlManually("TestPluginPage.hxaml");
    }

    protected override void LoadingOverride()
    {
        LoadViewModel(new TestPluginPageViewModel(TestPlugin.Instance.PluginContext.ServiceProvider));
    }
}
