#include "ModuleCommand.h"
#define WINRT_IMPORT_MODULE
import winrt_base;
import winrt.Windows.Foundation;
import winrt.Snap.Hutao.Plugin.Contracts;

int ReadCounterUsingModule(void* context)
{
    winrt::Snap::Hutao::Plugin::Contracts::IPluginContext projected{ nullptr };
    winrt::copy_from_abi(projected, context);
    return projected.Settings().GetInt32(L"Counter");
}
