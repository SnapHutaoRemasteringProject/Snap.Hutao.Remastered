#pragma once
#include "PluginPage.g.h"

namespace winrt::TestPlugin::Native::implementation
{
    struct PluginPage : PluginPageT<PluginPage>
    {
        PluginPage();
        void InitializePluginView(Snap::Hutao::Plugin::Contracts::IPluginContext const& context);
        void ClosePluginView();
        hstring TitleText() { return L"Compiled XAML / x:Bind"; }
        void OnClick(Windows::Foundation::IInspectable const&, Microsoft::UI::Xaml::RoutedEventArgs const&);

    private:
        // make_self detects accessible parameterless InitializeComponent and calls it.
        // Hide the generated host-relative loader; InitializePluginView owns resource loading.
        using PluginPageT<PluginPage>::InitializeComponent;

        Snap::Hutao::Plugin::Contracts::IPluginContext m_context{ nullptr };
    };
}

namespace winrt::TestPlugin::Native::factory_implementation
{
    struct PluginPage : PluginPageT<PluginPage, implementation::PluginPage> {};
}
