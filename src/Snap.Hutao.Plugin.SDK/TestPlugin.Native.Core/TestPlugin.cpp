#include "HutaoPluginAbi.h"
#include "ModuleCommand.h"
#include "WinRTModules.h"
#include "HutaoPlugin.hpp"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls;
using namespace Snap::Hutao::Plugin::Contracts;

// Independent core test: no compiled XAML metadata or PRI dependency.
struct CoreView : implements<CoreView, IPluginView>
{
    UIElement root{ nullptr };
    Button button{ nullptr };
    event_token clickToken{};
    bool connected{};

    CoreView(IPluginContext const& context, bool loose)
    {
        context.Log(NotificationKind::Information, loose ? L"[CppCore] Creating loose XAML view" : L"[CppCore] Creating programmatic view");
        if (loose)
        {
            root = context.Ui().LoadXaml(L"views/CoreView.hxaml");
            button = root.as<FrameworkElement>().FindName(L"ClickButton").as<Button>();
        }
        else
        {
            button = Button();
            button.Content(box_value(L"Programmatic C++/WinRT Button"));
            root = button;
        }
        clickToken = button.Click([context](winrt::Windows::Foundation::IInspectable const& sender, RoutedEventArgs const&)
        {
            try
            {
                sender.as<Button>().Content(box_value(L"C++/WinRT callback completed"));
                context.Notifications().Show(NotificationKind::Success, L"C++ core click");
            }
            catch (hresult_error const& error) { context.Log(NotificationKind::Error, error.message()); }
        });
        connected = true;
    }

    UIElement Element() { return root; }
    void OnClosing()
    {
        if (connected) { button.Click(clickToken); connected = false; }
        button = nullptr;
        root = nullptr;
    }
};

struct TestPluginImpl : implements<TestPluginImpl, IHutaoPlugin, IPluginViewFactory, IPluginCommand, IPluginAction>
{
    IPluginContext context{ nullptr };
    std::vector<IPluginRegistration> registrations;

    void OnLoad(IPluginContext const& value)
    {
        context = value;
        if (context.PluginId() != L"TestPlugin.Native.Core") throw hresult_invalid_argument(L"Core sample requires TestPlugin.Native.Core package.");
        context.Log(NotificationKind::Information, L"[CppCore] OnLoad");
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
        registrations.push_back(contributions.RegisterView(L"programmatic", self.as<IPluginViewFactory>()));
        registrations.push_back(contributions.RegisterNavigationItem(L"programmatic", L"C++ programmatic UI", L"\uE74C", L"programmatic"));
        registrations.push_back(contributions.RegisterView(L"loose", self.as<IPluginViewFactory>()));
        registrations.push_back(contributions.RegisterNavigationItem(L"loose", L"C++ loose XAML", L"\uE74C", L"loose"));
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
        if (id == L"programmatic") return make<CoreView>(context, false);
        if (id == L"loose") return make<CoreView>(context, true);
        throw hresult_invalid_argument();
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
