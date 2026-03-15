namespace Snap.Hutao.Remastered.API.Model.Plugin;

public class EditableSettingItem
{
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type ValueType { get; set; } = null!;
    public object? DefaultValue { get; set; }
    public object? CurrentValue { get; set; }
    public bool CanEdit { get; set; } = true;
}
