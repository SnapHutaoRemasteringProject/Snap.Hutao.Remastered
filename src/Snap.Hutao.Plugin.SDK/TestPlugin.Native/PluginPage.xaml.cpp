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

    void PluginPage::InitializePluginView(IPluginContext const& context)
    {
        if (_contentLoaded) throw hresult_illegal_method_call();
        m_context = context;
        // The compiler's default URI is relative to the host, not the plugin DLL.
        // Keep the generated Connect/GetBindingConnector and use the actual verified workspace.
        std::wstring path{ context.PackageDirectory().c_str() };
        for (auto& c : path) if (c == L'\\') c = L'/';
        if (path.size() < 3 || path[1] != L':' || path[2] != L'/')
            throw hresult_invalid_argument(L"Compiled XAML experiment requires a local drive path.");
        if (path.back() != L'/') path += L'/';
        path += L"views/PluginPage.xaml";
        std::wstring uri = L"ms-appx:///";
        uri += path.substr(0, 3);
        size_t start = 3;
        while (start < path.size())
        {
            auto end = path.find(L'/', start);
            auto segment = path.substr(start, end == std::wstring::npos ? end : end - start);
            auto escaped = Windows::Foundation::Uri::EscapeComponent(hstring{ segment });
            uri.append(escaped.c_str(), escaped.size());
            if (end == std::wstring::npos) break;
            uri += L'/';
            start = end + 1;
        }
        _contentLoaded = true;
        Application::LoadComponent(*this, Windows::Foundation::Uri{ uri }, ComponentResourceLocation::Application);
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
