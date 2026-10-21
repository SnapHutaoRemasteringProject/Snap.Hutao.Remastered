// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableTextContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string Text { get; set; }

    public static ObservableTextContent Create(TextContent textContent)
    {
        return new()
        {
            Text = textContent.Text,
        };
    }

    public static ObservableTextContent Create(string text)
    {
        return new()
        {
            Text = text,
        };
    }

    public void Update(ObservableTextContent textContent)
    {
        Text += textContent.Text;
    }
}
