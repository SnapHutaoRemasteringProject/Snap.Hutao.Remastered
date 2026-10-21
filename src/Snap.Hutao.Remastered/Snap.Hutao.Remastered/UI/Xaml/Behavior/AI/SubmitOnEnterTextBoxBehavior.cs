// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.AI;

[DependencyProperty<ICommand>("Command")]
[DependencyProperty<object>("CommandParameter")]
internal sealed partial class SubmitOnEnterTextBoxBehavior : BehaviorBase<TextBox>
{
    protected override bool Initialize()
    {
        if (!base.Initialize())
        {
            return false;
        }

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        return true;
    }

    protected override bool Uninitialize()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        return base.Uninitialize();
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not VirtualKey.Enter)
        {
            return;
        }

        if (IsLineBreakModifierDown())
        {
            InsertLineBreak(AssociatedObject);
            e.Handled = true;
            return;
        }

        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }

        e.Handled = true;
    }

    private static bool IsLineBreakModifierDown()
    {
        return IsKeyDown(VirtualKey.Shift) || IsKeyDown(VirtualKey.Control);
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
    }

    private static void InsertLineBreak(TextBox textBox)
    {
        // TODO: TextBlock uses ITextService2 which has better APIs for manipulating text, consider a hack in future.
        string text = textBox.Text ?? string.Empty;
        int selectionStart = textBox.SelectionStart;
        int selectionLength = textBox.SelectionLength;
        string newLine = Environment.NewLine;

        textBox.Text = text[..selectionStart] + newLine + text[(selectionStart + selectionLength)..];
        textBox.SelectionStart = selectionStart + newLine.Length;
        textBox.SelectionLength = 0;
    }
}
