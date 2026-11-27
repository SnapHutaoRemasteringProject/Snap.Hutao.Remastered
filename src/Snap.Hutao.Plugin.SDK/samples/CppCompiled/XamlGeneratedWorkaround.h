#pragma once

// Used only for XamlCompiler-generated metadata translation units via /FI.
// Establish native declarations before suppressing their repeated includes.
#include <windows.h>
#include <unknwn.h>
#include <inspectable.h>

#undef GetCurrentTime

#ifndef WINRT_IMPORT_MODULE
#define WINRT_IMPORT_MODULE
#endif
#ifndef __unknwn_h__
#define __unknwn_h__
#endif
#ifndef __RPC_H__
#define __RPC_H__
#endif
#ifndef __RPCNDR_H__
#define __RPCNDR_H__
#endif
#ifndef _INC_WINAPIFAMILY
#define _INC_WINAPIFAMILY
#endif
#ifndef _INC_WINPACKAGEFAMILY
#define _INC_WINPACKAGEFAMILY
#endif
#ifndef _OLE2_H_
#define _OLE2_H_
#endif
#ifndef _INC_WINDOWS
#define _INC_WINDOWS
#endif
#ifndef _INC_SDKDDKVER
#define _INC_SDKDDKVER
#endif

#undef _STL_COMPILER_PREPROCESSOR
#define _STL_COMPILER_PREPROCESSOR 0

import std;
import winrt_base;
import winrt.Windows.Foundation;
import winrt.Windows.Foundation.Collections;
import winrt.Windows.UI.Xaml.Interop;
import winrt.Microsoft.UI.Xaml;
import winrt.Microsoft.UI.Xaml.Controls;
import winrt.Microsoft.UI.Xaml.Markup;
import winrt.Microsoft.UI.Xaml.Interop;
import winrt.Microsoft.UI.Xaml.XamlTypeInfo;
import winrt.Snap.Hutao.Plugin.Contracts;
import winrt.TestPlugin.Native;
