using Snap.Hutao.Remastered.Web.Endpoint.Hutao;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public static class BydMaterialNameIconConverter
{
    public static Uri? IconNameToUri(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (name.StartsWith("UI_ItemIcon_", StringComparison.Ordinal))
        {
            return ItemIconConverter.IconNameToUri(name);
        }

        // "BeyondItemIcon" is the static-resource category published on the server. It is not
        // part of the model rename and must keep matching the published resources.
        return StaticResourcesEndpoints.StaticRaw("BeyondItemIcon", $"{name}.png").ToUri();
    }
}
