#include "HutaoPluginAbi.h"
#undef GetCurrentTime

#ifdef WINRT_IMPORT_MODULE
#include <restrictederrorinfo.h>
#include <hstring.h>

import winrt_base;
import winrt.Windows.Foundation;
import winrt.Microsoft.UI.Xaml;
import winrt.Microsoft.UI.Xaml.Controls;
import winrt.Microsoft.UI.Xaml.Controls.Primitives;
import winrt.Snap.Hutao.Plugin.Contracts;
#else
#include <winrt/Windows.Foundation.h>
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Snap.Hutao.Plugin.Contracts.h>
#endif

#include "HutaoPlugin.hpp"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls;
using namespace Snap::Hutao::Plugin::Contracts;

struct MyView : implements<MyView, IPluginView>
{
    Button button;
    event_token token;

    MyView(IPluginContext const& context)
    {
        button.Content(box_value(L"Click me"));
        token = button.Click([context](winrt::Windows::Foundation::IInspectable const&, RoutedEventArgs const&)
        {
            context.Notifications().Show(NotificationKind::Success, L"Hello from C++");
        });
    }

    UIElement Element() { return button; }
    void OnClosing() { button.Click(token); }
};

struct MyPlugin : implements<MyPlugin, IHutaoPlugin, IPluginViewFactory>
{
    IPluginContext context{ nullptr };
    IPluginRegistration view{ nullptr };
    IPluginRegistration navigation{ nullptr };

    void OnLoad(IPluginContext const& value) { context = value; }
    void OnInstall() {}
    void OnEnable()
    {
        auto contributions = context.Contributions();
        view = contributions.RegisterView(L"main", get_strong().as<IPluginViewFactory>());
        navigation = contributions.RegisterNavigationItem(L"main", L"My Native Plugin", L"\uE74C", L"main");
    }
    void OnDisable()
    {
        if (navigation) navigation.Revoke();
        if (view) view.Revoke();
        navigation = nullptr;
        view = nullptr;
    }
    void OnUninstall() {}
    IPluginView CreateView(hstring const& id)
    {
        if (id != L"main") throw hresult_invalid_argument();
        return make<MyView>(context);
    }
};

extern "C" __declspec(dllexport) HRESULT __cdecl HutaoPluginCreate(uint32_t abiMajor, ::IInspectable** result)
{
    return hutao::plugin::create<MyPlugin>(abiMajor, result);
}
