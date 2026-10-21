// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableChatMessage : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string? AuthorName { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? CreatedAt { get; set; }

    [ObservableProperty]
    public partial ChatRole Role { get; set; }

    [ObservableProperty]
    public partial ObservableAIContentCollection Contents { get; set; } = new();

    [ObservableProperty]
    public partial string? MessageId { get; set; }

    public static ObservableChatMessage Create(ChatMessage chatMessage, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            Role = chatMessage.Role,
            CreatedAt = chatMessage.CreatedAt,
            AuthorName = chatMessage.AuthorName,
            MessageId = chatMessage.MessageId,
            Contents = ObservableAIContentCollection.Create(chatMessage.Contents, jsonOptions) ?? [],
        };
    }

    public static ObservableChatMessage Create(ChatRole role, DateTimeOffset? createdAt, string? authorName = default, ObservableAIContent? content = default)
    {
        ObservableChatMessage observableChatMessage = new()
        {
            Role = role,
            CreatedAt = createdAt,
            AuthorName = authorName,
        };

        observableChatMessage.Contents.AddOrUpdate(content);
        return observableChatMessage;
    }
}
