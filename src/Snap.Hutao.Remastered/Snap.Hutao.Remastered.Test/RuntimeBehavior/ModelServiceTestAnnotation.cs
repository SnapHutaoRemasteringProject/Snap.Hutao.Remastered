namespace Snap.Hutao.Remastered.Service.AI;

// Only DI registration metadata is replaced in this source-linked test assembly.
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ServiceAttribute(Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, Type contract) : Attribute;
