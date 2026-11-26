// Compile-time smoke for the C++/WinRT 3.x named module projection.
// This is the same ABI and DLL, not another plugin model.
import winrt.Windows.Foundation;
import winrt.Snap.Hutao.Plugin.Contracts;

static_assert(sizeof(winrt::Snap::Hutao::Plugin::Contracts::IHutaoPlugin) == sizeof(void*));
