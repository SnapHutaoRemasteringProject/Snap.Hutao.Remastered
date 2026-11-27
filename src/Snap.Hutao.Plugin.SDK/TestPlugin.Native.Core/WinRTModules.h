#pragma once

// Native declarations must precede the module boundary. Unlike generated
// metadata sources, hand-written code must not fake Windows include guards.
#include <windows.h>
#include <unknwn.h>
#include <inspectable.h>
#include <restrictederrorinfo.h>
#include <hstring.h>
#include <cassert>
#include <functional>
#include <map>
#include <mutex>
#include <regex>

#undef GetCurrentTime

#ifndef WINRT_IMPORT_MODULE
#define WINRT_IMPORT_MODULE
#endif

// Prevent textual STL includes in generated XAML fragments after import std.
#undef _STL_COMPILER_PREPROCESSOR
#define _STL_COMPILER_PREPROCESSOR 0

import std;
import winrt_base;
import winrt.Windows.Foundation;
import winrt.Windows.Foundation.Collections;
import winrt.Windows.ApplicationModel.Activation;
import winrt.Windows.UI.Xaml.Interop;
import winrt.Microsoft.UI.Composition;
import winrt.Microsoft.UI.Xaml;
import winrt.Microsoft.UI.Xaml.Controls;
import winrt.Microsoft.UI.Xaml.Controls.Primitives;
import winrt.Microsoft.UI.Xaml.Data;
import winrt.Microsoft.UI.Xaml.Interop;
import winrt.Microsoft.UI.Xaml.Markup;
import winrt.Microsoft.UI.Xaml.Media;
import winrt.Microsoft.UI.Xaml.Navigation;
import winrt.Microsoft.UI.Xaml.Shapes;
import winrt.Microsoft.UI.Xaml.XamlTypeInfo;
import winrt.Microsoft.UI.Dispatching;
import winrt.Snap.Hutao.Plugin.Contracts;
