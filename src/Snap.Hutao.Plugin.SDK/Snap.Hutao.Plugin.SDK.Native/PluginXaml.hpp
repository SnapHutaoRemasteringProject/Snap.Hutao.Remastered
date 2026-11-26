#pragma once

// Experimental compiled-XAML URI helper. Use the same header/module mode as the plugin.
#ifndef WINRT_IMPORT_MODULE
#include <string>
#include <string_view>
#include <winrt/Windows.Foundation.h>
#else
import std;
import winrt.Windows.Foundation;
#endif

namespace hutao::plugin
{
    // logicalPath is the .xaml URI corresponding to the deployed .xbf, not the XBF filename.
    inline winrt::Windows::Foundation::Uri compiled_xaml_uri(
        winrt::hstring const& packageDirectory, std::wstring_view logicalPath)
    {
        if (logicalPath.empty() || !logicalPath.ends_with(L".xaml") ||
            logicalPath.find_first_of(L"\\:") != std::wstring_view::npos)
            throw winrt::hresult_invalid_argument(L"Expected a package-relative .xaml path.");
        for (size_t start = 0;;)
        {
            size_t end = logicalPath.find(L'/', start);
            auto segment = logicalPath.substr(start, end == std::wstring_view::npos ? end : end - start);
            if (segment.empty() || segment == L"." || segment == L"..")
                throw winrt::hresult_invalid_argument(L"Invalid compiled XAML path segment.");
            if (end == std::wstring_view::npos) break;
            start = end + 1;
        }

        std::wstring path{ packageDirectory.c_str() };
        for (auto& c : path) if (c == L'\\') c = L'/';
        if (path.size() < 3 || path[1] != L':' || path[2] != L'/')
            throw winrt::hresult_invalid_argument(L"Compiled XAML experiment requires a local drive path.");
        if (path.back() != L'/') path += L'/';
        path.append(logicalPath);

        std::wstring uri = L"ms-appx:///";
        uri += path.substr(0, 3);
        for (size_t start = 3; start < path.size();)
        {
            size_t end = path.find(L'/', start);
            auto segment = path.substr(start, end == std::wstring::npos ? end : end - start);
            auto escaped = winrt::Windows::Foundation::Uri::EscapeComponent(winrt::hstring{ segment });
            uri.append(escaped.c_str(), escaped.size());
            if (end == std::wstring::npos) break;
            uri += L'/';
            start = end + 1;
        }
        return winrt::Windows::Foundation::Uri{ winrt::hstring{ uri } };
    }
}
