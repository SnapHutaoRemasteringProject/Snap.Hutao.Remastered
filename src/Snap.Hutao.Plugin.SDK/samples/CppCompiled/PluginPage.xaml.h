#pragma once
#include "PluginPage.g.h"

namespace winrt::TestPlugin::Native::implementation
{
    struct PluginPage : PluginPageT<PluginPage>
    {
        PluginPage();
        // make_self calls this hook after construction. InitializePluginView calls the generated loader.
        void InitializeComponent();
        void InitializePluginView(Snap::Hutao::Plugin::Contracts::IPluginContext const& context);
        void ClosePluginView();
        hstring TitleText() { return L"Compiled XAML / x:Bind"; }
        void OnClick(Windows::Foundation::IInspectable const&, Microsoft::UI::Xaml::RoutedEventArgs const&);

    private:
        Snap::Hutao::Plugin::Contracts::IPluginContext m_context{ nullptr };
    };
}

namespace winrt::TestPlugin::Native::factory_implementation
{
    struct PluginPage : PluginPageT<PluginPage, implementation::PluginPage> {};
}
