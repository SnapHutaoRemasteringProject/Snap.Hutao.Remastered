// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Service.AI.Observables;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

internal sealed class ChatToolResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CodeInterpreterTemplate { get; set; }

    public DataTemplate? FunctionTemplate { get; set; }

    public DataTemplate? ImageGenerationTemplate { get; set; }

    public DataTemplate? McpServerTemplate { get; set; }

    public DataTemplate? WebSearchTemplate { get; set; }

    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            ObservableCodeInterpreterToolResultContent => CodeInterpreterTemplate,
            ObservableFunctionResultContent => FunctionTemplate,
            ObservableImageGenerationToolResultContent => ImageGenerationTemplate,
            ObservableMcpServerToolResultContent => McpServerTemplate,
            ObservableWebSearchToolResultContent => WebSearchTemplate,
            _ => FallbackTemplate,
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
