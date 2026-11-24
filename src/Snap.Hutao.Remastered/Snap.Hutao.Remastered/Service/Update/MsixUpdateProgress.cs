// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using DevWinUI;
using Microsoft.UI.Dispatching;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton)]
public sealed class MsixUpdateProgress(ITaskContext taskContext)
{
    private GrowlHost? host;
    private GrowlHandle? card;
    private DispatcherQueue? dispatcher;
    private PackageCatalog? catalog;
    private Guid? windowsUpdate;
    private bool manualUpdate;
    private bool restartPending;

    // All state is read and written on the main dispatcher.
    public bool IsUpdating => manualUpdate || windowsUpdate.HasValue || restartPending;

    public void Attach(GrowlHost growlHost)
    {
        if (ReferenceEquals(host, growlHost))
        {
            return;
        }

        if (host is { } previousHost)
        {
            Detach(previousHost);
        }

        host = growlHost;
        Volatile.Write(ref dispatcher, growlHost.DispatcherQueue);
        if (restartPending)
        {
            ShowRestartNotice();
        }

        if (!RuntimeEnvironment.IsPackaged)
        {
            return;
        }

        try
        {
            catalog = PackageCatalog.OpenForCurrentPackage();
            catalog.PackageUpdating += OnPackageUpdating;
        }
        catch (Exception exception)
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug($"Package update monitoring unavailable ({exception.HResult:X8})", "update"));
        }
    }

    public void Detach(GrowlHost growlHost)
    {
        if (!ReferenceEquals(host, growlHost))
        {
            return;
        }

        if (catalog is { } currentCatalog)
        {
            currentCatalog.PackageUpdating -= OnPackageUpdating;
            catalog = null;
        }

        CloseCard();
        windowsUpdate = null;
        Volatile.Write(ref dispatcher, null);
        host = null;
    }

    public async ValueTask RunAsync(Func<IProgress<DeploymentProgress>, Task> deploy)
    {
        await taskContext.SwitchToMainThreadAsync();
        if (IsUpdating || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        manualUpdate = true;
        try
        {
            GrowlHandle? progressCard = ShowProgress(SH.ServiceUpdateMsixDeploymentTitle);
            Progress<DeploymentProgress> progress = new(value => progressCard?.Update(
                progress: value.percentage,
                isProgressIndeterminate: value.state == DeploymentProgressState.Queued));
            await deploy(progress);
            restartPending = RuntimeEnvironment.IsPackaged;
            ShowRestartNotice();
        }
        catch
        {
            CloseCard();
            throw;
        }
        finally
        {
            manualUpdate = false;
        }
    }

    private void OnPackageUpdating(PackageCatalog sender, PackageUpdatingEventArgs args)
    {
        if (Volatile.Read(ref manualUpdate))
        {
            return;
        }

        // The catalog also raises events for related packages. Only track the app being upgraded.
        PackageId running = Package.Current.Id;
        if (args.TargetPackage?.Id is not { } target || target.FamilyName != running.FamilyName || target.FullName == running.FullName)
        {
            return;
        }

        Guid activity = args.ActivityId;
        double progress = Math.Clamp(args.Progress, 0, 1) * 100;
        bool complete = args.IsComplete;
        bool failed = args.ErrorCode is { HResult: < 0 };
        Volatile.Read(ref dispatcher)?.TryEnqueue(() => ReportWindowsUpdate(sender, activity, progress, complete, failed));
    }

    private void ReportWindowsUpdate(PackageCatalog sender, Guid activity, double progress, bool complete, bool failed)
    {
        if (!ReferenceEquals(sender, catalog) || manualUpdate || restartPending || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        if (complete)
        {
            if (windowsUpdate is { } tracked && tracked != activity)
            {
                return;
            }

            windowsUpdate = null;
            if (failed)
            {
                ShowNotice(SH.ServiceUpdateMsixWindowsDeploymentFailed, GrowlCardSeverity.Warning);
                return;
            }

            restartPending = true;
            ShowRestartNotice();
            return;
        }

        if (windowsUpdate != activity)
        {
            windowsUpdate = activity;
            ShowProgress(SH.ServiceUpdateMsixWindowsDeploymentTitle);
        }

        card?.Update(progress: progress, isProgressIndeterminate: false);
    }

    private GrowlHandle? ShowProgress(string title)
    {
        CloseCard();
        card = host?.Show(new GrowlOptions
        {
            Title = title,
            Severity = GrowlCardSeverity.Info,
            AutoClose = GrowlCardAutoClose.None,
            ShowCloseButton = false,
            ShowProgress = true,
            IsProgressIndeterminate = true,
        });
        return card;
    }

    private void ShowRestartNotice()
    {
        ShowNotice(RuntimeEnvironment.IsPackaged ? SH.ServiceUpdateMsixRestart : SH.ServiceUpdateMsixOpenPackaged, GrowlCardSeverity.Success);
    }

    private void ShowNotice(string title, GrowlCardSeverity severity)
    {
        CloseCard();
        card = host?.Show(new GrowlOptions
        {
            Title = title,
            Severity = severity,
            AutoClose = GrowlCardAutoClose.None,
        });
    }

    private void CloseCard()
    {
        card?.Close();
        card = null;
    }
}
