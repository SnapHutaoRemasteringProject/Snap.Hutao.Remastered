// Source-linked handler tests replace DI registration metadata and the domain
// service boundary. HTTP requests execute the production handler without network.
namespace Snap.Hutao.Remastered.Core.IO.Http
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ServiceAttribute : Attribute
    {
        public ServiceAttribute(Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { }
    }
}

namespace Snap.Hutao.Remastered.Web
{
    public interface IServerDomainService
    {
        bool IsBackupMode();

        Uri? RewriteHostToCurrentMode(Uri? uri);
    }
}
