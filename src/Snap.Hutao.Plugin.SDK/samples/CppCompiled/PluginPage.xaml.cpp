#include "XamlWorkaround.h"
#include "PluginPage.xaml.h"
#include "PluginXaml.hpp"
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
        // Deliberately hide the generated host-relative loader. Do not set _contentLoaded here.
        // The verified package directory is only available in InitializePluginView.
        ::OutputDebugStringW(L"[TestPlugin.Native] PluginPage::InitializeComponent deferred\n");
    }

    void PluginPage::InitializePluginView(IPluginContext const& context)
    {
        if (_contentLoaded) throw hresult_illegal_method_call();
        m_context = context;
        // The compiler's default URI is relative to the host, not the plugin DLL.
        // Keep the generated Connect/GetBindingConnector and use the actual verified workspace.
        auto uri = hutao::plugin::compiled_xaml_uri(context.PackageDirectory(), L"views/PluginPage.xaml");
        std::wstring message{ L"[TestPlugin.Native] LoadComponent URI: " };
        message += uri.AbsoluteUri().c_str();
        context.Log(NotificationKind::Information, hstring{ message });
        _contentLoaded = true;
        Application::LoadComponent(*this, uri, ComponentResourceLocation::Application);
        context.Log(NotificationKind::Success, L"[TestPlugin.Native] LoadComponent returned successfully");
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
