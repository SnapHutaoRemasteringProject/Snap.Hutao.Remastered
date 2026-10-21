// Only the exception factory for the unused IniSection.Builder.ToString path
// is substituted. The INI parser, version inspection and file repair are real.
global using Snap.Hutao.Remastered.Extension;

namespace Snap.Hutao.Remastered.Core.ExceptionService;

internal static class HutaoException
{
    public static NotSupportedException NotSupported()
    {
        return new();
    }
}
