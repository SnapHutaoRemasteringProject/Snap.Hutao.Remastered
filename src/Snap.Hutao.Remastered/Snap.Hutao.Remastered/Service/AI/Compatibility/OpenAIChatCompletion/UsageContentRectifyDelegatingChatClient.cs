// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Service.AI.Compatibility.OpenAIChatCompletion;

// Specially designed DelegatingChatClient to rectify multiple 'usage_content' in one chat response.
internal sealed class UsageContentRectifyDelegatingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);

        ChatMessage? lastMessage = null;
        int lastIndex = -1;

        foreach (ChatMessage message in response.Messages)
        {
            for (int i = 0; i < message.Contents.Count; i++)
            {
                if (message.Contents[i] is not UsageContent)
                {
                    continue;
                }

                if (lastMessage is not null)
                {
                    lastMessage.Contents.RemoveAt(lastIndex);

                    if (ReferenceEquals(lastMessage, message) && lastIndex < i)
                    {
                        i--;
                    }
                }

                lastMessage = message;
                lastIndex = i;
            }
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using IAsyncEnumerator<ChatResponseUpdate> enumerator = base
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        if (!await enumerator.MoveNextAsync())
        {
            yield break;
        }

        ChatResponseUpdate current = enumerator.Current;

        while (await enumerator.MoveNextAsync())
        {
            ChatResponseUpdate next = enumerator.Current;

            if (ContainsUsageContent(current) && ContainsUsageContent(next))
            {
                RemoveUsageContent(current);
            }

            yield return current;
            current = next;
        }

        yield return current;
    }

    private static bool ContainsUsageContent(ChatResponseUpdate update)
    {
        return update.Contents.Any(static content => content is UsageContent);
    }

    private static void RemoveUsageContent(ChatResponseUpdate update)
    {
        // Backwards iteration to safely remove items while iterating
        for (int i = update.Contents.Count - 1; i >= 0; i--)
        {
            if (update.Contents[i] is UsageContent)
            {
                update.Contents.RemoveAt(i);
            }
        }
    }
}
