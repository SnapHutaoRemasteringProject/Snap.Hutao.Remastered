using Snap.Hutao.Remastered.ViewModel.Abstraction;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

// ScopedPage owns this ViewModel and the main navigation observes IsInitialized.
[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class PluginViewModel : Abstraction.ViewModel
{
    internal void MarkReady() => IsInitialized = true;
}
