// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

[DependencyProperty<IReadOnlyList<IReadOnlyList<string>>>("Rows", PropertyChangedCallbackName = nameof(OnTableChanged))]
[DependencyProperty<IReadOnlyList<global::Microsoft.UI.Xaml.TextAlignment>>("ColumnAlignments", PropertyChangedCallbackName = nameof(OnTableChanged))]
internal sealed partial class MarkdownTableView : UserControl
{
    public MarkdownTableView()
    {
        InitializeComponent();
    }

    private static void OnTableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTableView view)
        {
            view.RebuildRows();
        }
    }

    private void RebuildRows()
    {
        TableGrid.RowDefinitions.Clear();
        TableGrid.ColumnDefinitions.Clear();
        TableGrid.Children.Clear();

        if (Rows is null || Rows.Count == 0)
        {
            return;
        }

        int columnCount = Rows.Max(row => row.Count);
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            TableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1.0, GridUnitType.Star) });
        }

        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            TableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            IReadOnlyList<string> row = Rows[rowIndex];

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                AddCell(row, rowIndex, columnIndex, columnCount);
            }
        }
    }

    private void AddCell(IReadOnlyList<string> row, int rowIndex, int columnIndex, int columnCount)
    {
        RichTextBlock cellText = new()
        {
            FontSize = 13,
            IsTextSelectionEnabled = true,
            TextAlignment = GetCellTextAlignment(columnIndex),
            TextWrapping = TextWrapping.Wrap,
        };

        Paragraph paragraph = new() { Margin = new Thickness(0) };
        MarkdownHelper.AddInlineMarkdown(paragraph.Inlines, columnIndex < row.Count ? row[columnIndex] : string.Empty);
        if (rowIndex == 0)
        {
            paragraph.FontWeight = FontWeights.SemiBold;
        }

        cellText.Blocks.Add(paragraph);

        Border cellBorder = new()
        {
            Child = cellText,
            Padding = new Thickness(8, 4, 8, 4),
            BorderBrush = MarkdownHelper.GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            BorderThickness = new Thickness(
                0,
                0,
                columnIndex < columnCount - 1 ? 1 : 0,
                rowIndex == 0 ? 1 : 0),
        };

        Grid.SetRow(cellBorder, rowIndex);
        Grid.SetColumn(cellBorder, columnIndex);
        TableGrid.Children.Add(cellBorder);
    }

    private global::Microsoft.UI.Xaml.TextAlignment GetCellTextAlignment(int columnIndex)
    {
        if (ColumnAlignments is not null && columnIndex < ColumnAlignments.Count)
        {
            return ColumnAlignments[columnIndex];
        }

        return global::Microsoft.UI.Xaml.TextAlignment.Left;
    }
}
