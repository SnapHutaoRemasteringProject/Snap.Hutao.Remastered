using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Snap.Hutao.Plugin.Services
{
    // Public ABI. The host supplies a service instance; this is not an activatable class.
    [Guid("7BB6A48C-5F18-49EA-BF34-0AE91C2DF587")]
    public interface IHutaoGamePathService
    {
        // Empty string means the host service cannot locate the game.
        // The operation may synchronize the host's detected game path.
        IAsyncOperation<string> LocateAsync();
    }
}
