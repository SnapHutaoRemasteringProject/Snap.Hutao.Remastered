global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using System.Diagnostics.CodeAnalysis;

using Quartz;

namespace Snap.Hutao.Remastered.Service.Job;

// The portable test assembly links the production service rather than the
// Windows application. Supply only its generated DI constructor/annotations.
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute
{
    public ServiceAttribute(ServiceLifetime lifetime, Type serviceType) { }
}

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class GeneratedConstructorAttribute : Attribute;

public sealed partial class QuartzService
{
    public partial QuartzService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
        logger = serviceProvider.GetRequiredService<ILogger<QuartzService>>();
    }
}

public static class QuartzTestTaskExtension
{
    public static void SafeForget(this Task task)
    {
        _ = task.ContinueWith(static completed => _ = completed.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
