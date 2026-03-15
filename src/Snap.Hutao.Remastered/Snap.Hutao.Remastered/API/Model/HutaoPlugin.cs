namespace Snap.Hutao.Remastered.API.Model;

public abstract class HutaoPlugin
{
    /// <summary>
    /// 不要手动设置此状态 应该通过PluginService来管理插件状态
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// 不要手动设置此状态 应该通过PluginService来管理插件状态
    /// </summary>
    public PluginManifest Manifest { get; set; } = new PluginManifest();

    /// <summary>
    /// 不要手动设置此状态 应该通过PluginService来管理插件状态
    /// </summary>
    public string IconPath { get; set; } = string.Empty;

    public abstract void OnInstall();

    public abstract void OnLoad(PluginContext context);

    public abstract void OnEnable();

    public abstract void OnDisable();

    public abstract void OnUninstall();
}
