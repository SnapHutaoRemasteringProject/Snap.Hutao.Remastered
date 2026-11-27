#include "XamlWorkaround.h"
#include "PluginPage.xaml.h"
#if __has_include("PluginPage.g.cpp")
#include "PluginPage.g.cpp"
#endif

using namespace winrt;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls::Primitives;
using namespace Snap::Hutao::Plugin::Contracts;

namespace winrt::TestPlugin::Native::implementation
{
    PluginPage::PluginPage() = default;

    void PluginPage::InitializeComponent()
    {
        // Defer until the view has its plugin context. The generated initializer
        // will resolve its normal component URI through the host's MRT manager.
        ::OutputDebugStringW(L"[TestPlugin.Native] PluginPage::InitializeComponent deferred\n");
    }

    void PluginPage::InitializePluginView(IPluginContext const& context)
    {
        if (_contentLoaded) throw hresult_illegal_method_call();
        m_context = context;
        // The host has registered the component PRI before OnLoad.
        // Use the standard compiler-generated logical URI and connector code.
        PluginPageT<PluginPage>::InitializeComponent();
        context.Log(NotificationKind::Success, L"[TestPlugin.Native] Generated InitializeComponent returned");
    }

    void PluginPage::ClosePluginView()
    {
        m_context = nullptr;
    }

    void PluginPage::OnClick(Windows::Foundation::IInspectable const&, RoutedEventArgs const&)
    {
        if (!m_context) return;
        StatusText().Text(L"Generated code-behind Click connected");
        m_context.Notifications().Show(NotificationKind::Success, L"Compiled C++ XAML click");
    }
}
