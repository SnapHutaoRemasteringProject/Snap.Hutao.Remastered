#pragma once
#include <windows.h>
#include <inspectable.h>
#include <stdint.h>
#ifdef __cplusplus
extern "C" {
#endif
__declspec(dllexport) HRESULT __cdecl HutaoPluginCreate(uint32_t abiMajor, IInspectable** plugin);
#ifdef __cplusplus
}
#endif
