using Snap.Hutao.Remastered.Web.Endpoint.Hutao;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public static class BeyondItemNameIconConverter
{

    public static Uri IconNameToUri(string name)
    {
        return StaticResourcesEndpoints.StaticRaw("BeyondItemIcon", $"{name}.png").ToUri();
    }
}
