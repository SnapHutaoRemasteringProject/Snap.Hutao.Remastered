// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.
// Conversation layout follows the role/segment approach of DGP-Studio/Snap.Nicole AgentPage.
// Only local WinUI primitives are used to avoid pulling in an unrelated agent runtime.

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private readonly StringBuilder responseBuffer = new();
    private TextBlock? streamingResponse;
    private StackPanel? activeMessagePanel;
    private long lastUiFlushTime;
    private int lastUiFlushLength;

    private void BeginConversationMessage(bool test)
    {
        responseBuffer.Clear();
        lastUiFlushLength = 0;
        lastUiFlushTime = 0;

        // Keep a readable transcript of requests/results without exposing raw account data.
        AddConversationCard(test ? L("TestConnection") : L("Generate"), string.Empty, false);

        activeMessagePanel = new StackPanel { Spacing = 8 };
        activeMessagePanel.Children.Add(new TextBlock
        {
            Text = L("AssistantLabel"),
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.7,
        });

        streamingResponse = new TextBlock
        {
            Text = L("Sending"),
            TextWrapping = TextWrapping.WrapWholeWords,
            IsTextSelectionEnabled = true,
        };
        activeMessagePanel.Children.Add(streamingResponse);
        ConversationPanel.Children.Add(new Border
        {
            Style = (Style)RootGrid.Resources["InsightAssistantCardStyle"],
            Child = activeMessagePanel,
        });
        ScrollToLatestMessage();
    }

    private void AddConversationCard(string title, string detail, bool assistant)
    {
        StackPanel content = new() { Spacing = 4 };
        content.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.75,
        });
        if (detail.Length > 0)
        {
            content.Children.Add(new TextBlock { Text = detail, TextWrapping = TextWrapping.Wrap });
        }

        ConversationPanel.Children.Add(new Border
        {
            HorizontalAlignment = assistant ? HorizontalAlignment.Stretch : HorizontalAlignment.Right,
            MaxWidth = 740,
            Style = (Style)RootGrid.Resources["InsightUserCardStyle"],
            Child = content,
        });
    }

    private void AppendResponse(string chunk)
    {
        if (chunk.Length == 0)
        {
            return;
        }

        // Bound UI memory for a provider that returns unexpectedly large responses.
        int available = Math.Max(0, 262144 - responseBuffer.Length);
        if (available == 0)
        {
            return;
        }

        responseBuffer.Append(chunk.AsSpan(0, Math.Min(chunk.Length, available)));
        long now = Environment.TickCount64;
        if (responseBuffer.Length - lastUiFlushLength >= 80 || now - lastUiFlushTime >= 100)
        {
            FlushResponse();
        }
    }

    private void FlushResponse()
    {
        if (streamingResponse is null)
        {
            return;
        }

        streamingResponse.Text = responseBuffer.Length > 0 ? responseBuffer.ToString() : L("Sending");
        lastUiFlushLength = responseBuffer.Length;
        lastUiFlushTime = Environment.TickCount64;
        ScrollToLatestMessage();
    }

    private void CompleteResponse(string? usageText = null)
    {
        FlushResponse();
        if (streamingResponse is null || activeMessagePanel is null || responseBuffer.Length == 0)
        {
            return;
        }

        int index = activeMessagePanel.Children.IndexOf(streamingResponse);
        if (index >= 0)
        {
            activeMessagePanel.Children.RemoveAt(index);
            activeMessagePanel.Children.Insert(index, RenderMarkdownResponse(responseBuffer.ToString()));
        }

        StackPanel actions = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        Button copy = new() { MinWidth = 32, Padding = new Thickness(8), Content = L("CopyResponse") };
        string answer = responseBuffer.ToString();
        copy.Click += (_, _) =>
        {
            DataPackage package = new();
            package.SetText(answer);
            Clipboard.SetContent(package);
        };
        actions.Children.Add(copy);
        if (!string.IsNullOrWhiteSpace(usageText))
        {
            actions.Children.Add(new TextBlock
            {
                Text = usageText,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.7,
            });
        }
        activeMessagePanel.Children.Add(actions);
        ScrollToLatestMessage();
    }

    private void ShowResponseError(string message)
    {
        if (streamingResponse is not null && responseBuffer.Length == 0)
        {
            streamingResponse.Text = message;
        }
        else
        {
            FlushResponse();
            activeMessagePanel?.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        ScrollToLatestMessage();
    }

    private void ScrollToLatestMessage()
    {
        ConversationScrollViewer.ChangeView(null, ConversationScrollViewer.ScrollableHeight, null);
    }

    private static RichTextBlock RenderMarkdownResponse(string content)
    {
        // A constrained Markdown subset: headings, lists, emphasis and inline code.
        // No remote HTML/images/scripts or navigation is executed from model output.
        RichTextBlock view = new()
        {
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.Wrap,
        };
        bool fenced = false;
        foreach (string sourceLine in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = sourceLine.TrimEnd();
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                continue;
            }

            if (line.Length == 0)
            {
                continue;
            }

            Paragraph paragraph = new() { Margin = new Thickness(0, 3, 0, 3) };
            if (fenced)
            {
                paragraph.FontFamily = new FontFamily("Consolas");
                paragraph.Inlines.Add(new Run { Text = line });
            }
            else
            {
                string trimmed = line.TrimStart();
                int heading = 0;
                while (heading < trimmed.Length && trimmed[heading] == '#')
                {
                    heading++;
                }

                if (heading is > 0 and <= 6 && heading < trimmed.Length && trimmed[heading] == ' ')
                {
                    paragraph.FontWeight = FontWeights.SemiBold;
                    paragraph.FontSize = heading switch { 1 => 23, 2 => 19, 3 => 16, _ => 14 };
                    trimmed = trimmed[(heading + 1)..];
                }
                else if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
                {
                    trimmed = "• " + trimmed[2..];
                    paragraph.Margin = new Thickness(12, 3, 0, 3);
                }

                int last = 0;
                foreach (Match match in Regex.Matches(trimmed, @"\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`"))
                {
                    if (match.Index > last)
                    {
                        paragraph.Inlines.Add(new Run { Text = trimmed[last..match.Index] });
                    }

                    string span = match.Value;
                    Run run = new() { Text = span.StartsWith("**", StringComparison.Ordinal) ? span[2..^2] : span[1..^1] };
                    if (span.StartsWith("**", StringComparison.Ordinal))
                    {
                        run.FontWeight = FontWeights.Bold;
                    }
                    else if (span.StartsWith('*'))
                    {
                        run.FontStyle = global::Windows.UI.Text.FontStyle.Italic;
                    }
                    else
                    {
                        run.FontFamily = new FontFamily("Consolas");
                    }

                    paragraph.Inlines.Add(run);
                    last = match.Index + match.Length;
                }

                if (last < trimmed.Length)
                {
                    paragraph.Inlines.Add(new Run { Text = trimmed[last..] });
                }
            }

            view.Blocks.Add(paragraph);
        }

        return view;
    }
}
