#include "HutaoPluginAbi.h"
#include "ModuleCommand.h"
#include "XamlWorkaround.h"
#include "PluginPage.xaml.h"
#include "HutaoPlugin.hpp"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls;
using namespace Snap::Hutao::Plugin::Contracts;

struct TestView : implements<TestView, IPluginView>
{
    winrt::com_ptr<winrt::TestPlugin::Native::implementation::PluginPage> page;

    explicit TestView(IPluginContext const& context)
    {
        context.Log(NotificationKind::Information, L"[TestPlugin.Native] Creating Page (PRI-backed generated initializer)");
        page = winrt::make_self<winrt::TestPlugin::Native::implementation::PluginPage>();
        context.Log(NotificationKind::Information, L"[TestPlugin.Native] make_self returned; initializing from component PRI");
        page->InitializePluginView(context);
    }

    UIElement Element() { return page.as<UIElement>(); }
    void OnClosing() { page->ClosePluginView(); }
};

struct TestPluginImpl : implements<TestPluginImpl, IHutaoPlugin, IPluginViewFactory, IPluginCommand, IPluginAction>
{
    IPluginContext context{ nullptr };
    std::vector<IPluginRegistration> registrations;

    void OnLoad(IPluginContext const& value)
    {
        context = value;
        if (context.PluginId() != L"TestPlugin.Native") throw hresult_invalid_argument(L"Compiled sample requires TestPlugin.Native package.");
        context.Log(NotificationKind::Information, L"[CppCompiled] OnLoad");
        if (value.AbiMajor() != 1 || value.ApiMinor() != 0) throw hresult_invalid_argument();
        value.Settings().RegisterInt32(L"Counter", 0, L"Counter");
        value.Settings().RegisterBoolean(L"Enabled", true, L"Boolean");
        value.Settings().RegisterString(L"Message", L"Default Message", L"Message");
        auto notifications = value.GetService(guid_of<IPluginNotifications>()).as<IPluginNotifications>();
        notifications.Show(NotificationKind::Success, L"C++/WinRT OnLoad");
        if (value.GetService(guid{})) throw hresult_error(E_FAIL, L"Unknown service must be null.");
    }

    void OnInstall() { context.Log(NotificationKind::Information, L"C++/WinRT OnInstall"); }
    void OnEnable()
    {
        auto contributions = context.Contributions();
        auto self = get_strong();
        registrations.push_back(contributions.RegisterView(L"main", self.as<IPluginViewFactory>()));
        registrations.push_back(contributions.RegisterNavigationItem(L"main", L"C++/WinRT plugin", L"\uE74C", L"main"));
        registrations.push_back(contributions.RegisterCommand(L"exercise", L"Exercise ABI", self.as<IPluginCommand>()));
    }

    void OnDisable()
    {
        for (auto const& registration : registrations) registration.Revoke();
        registrations.clear();
    }

    void OnUninstall()
    {
        context.Log(NotificationKind::Information, L"C++/WinRT OnUninstall");
        context = nullptr;
    }

    IPluginView CreateView(hstring const& id)
    {
        if (id != L"main") throw hresult_invalid_argument();
        return make<TestView>(context);
    }

    IAsyncAction ExecuteAsync(hstring id)
    {
        auto lifetime = get_strong();
        if (id != L"exercise") throw hresult_invalid_argument();
        auto settings = context.Settings();
        co_await settings.SetInt32Async(L"Counter", ReadCounterUsingModule(get_abi(context)) + 1);
        co_await settings.SetBooleanAsync(L"Enabled", false);
        co_await settings.SetStringAsync(L"Message", L"C++/WinRT round trip");
        if (settings.GetBoolean(L"Enabled") || settings.GetString(L"Message") != L"C++/WinRT round trip")
            throw hresult_error(E_FAIL, L"Settings round trip failed.");
        auto dispatcher = context.Dispatcher();
        auto action = lifetime.as<IPluginAction>();
        if (!dispatcher.try_as<IAgileObject>() || !action.try_as<IAgileObject>())
            throw hresult_error(E_NOINTERFACE, L"Cross-thread interfaces must be agile.");
        apartment_context ui;
        co_await resume_background();
        if (dispatcher.HasThreadAccess()) throw hresult_error(E_FAIL, L"Expected worker.");
        co_await dispatcher.InvokeAsync(action);
        co_await ui;
        auto result = co_await context.Dialogs().ShowMessageAsync(L"C++/WinRT ABI", L"Choose OK to complete the test.", L"OK", L"Cancel");
        if (result != DialogResult::Primary) throw hresult_canceled();
        context.Notifications().Show(NotificationKind::Success, L"C++/WinRT async round trip completed.");
    }

    void Invoke()
    {
        if (!context.Dispatcher().HasThreadAccess()) throw hresult_error(RPC_E_WRONG_THREAD);
        context.Log(NotificationKind::Information, L"C++/WinRT worker callback on UI thread");
    }
};

extern "C" __declspec(dllexport) HRESULT __cdecl HutaoPluginCreate(uint32_t abiMajor, ::IInspectable** plugin)
{
    return hutao::plugin::create<TestPluginImpl>(abiMajor, plugin);
}
