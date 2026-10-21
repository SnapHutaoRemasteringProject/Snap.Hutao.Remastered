// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Service.AI.Observables;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

internal sealed class ChatContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? ReasoningTemplate { get; set; }

    public DataTemplate? ToolCallTemplate { get; set; }

    public DataTemplate? ToolResultTemplate { get; set; }

    public DataTemplate? UsageTemplate { get; set; }

    public DataTemplate? ErrorTemplate { get; set; }

    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            ObservableTextContent => TextTemplate,
            ObservableTextReasoningContent => ReasoningTemplate,
            ObservableToolCallContent => ToolCallTemplate,
            ObservableToolResultContent => ToolResultTemplate,
            ObservableUsageContent => UsageTemplate,
            ObservableErrorContent => ErrorTemplate,
            _ => FallbackTemplate,
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
