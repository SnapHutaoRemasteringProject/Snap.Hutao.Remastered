#pragma once
#include "HutaoPluginAbi.h"
#ifdef GetCurrentTime
#undef GetCurrentTime
#endif
#include <winrt/Windows.Foundation.h>
#include <winrt/Snap.Hutao.Plugin.Contracts.h>

namespace hutao::plugin
{
    template <typename T>
    HRESULT create(uint32_t abiMajor, ::IInspectable** result) noexcept
    {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (abiMajor != 1) return HRESULT_FROM_WIN32(ERROR_REVISION_MISMATCH);
        try
        {
            auto instance = winrt::make<T>();
            auto inspectable = instance.template as<winrt::Windows::Foundation::IInspectable>();
            *result = reinterpret_cast<::IInspectable*>(winrt::detach_abi(inspectable));
            return S_OK;
        }
        catch (...) { return winrt::to_hresult(); }
    }
}
