// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.IO;

namespace Snap.Hutao.Remastered.Service.AI.Migration;

internal static class AgentLegacyMigration
{
    public static void ImportProfiles(string directory, AgentSettings settings, string fallbackEndpoint, string fallbackModel)
    {
        string path = Path.Combine(directory, "profiles.json");
        List<LegacyModelProfile> profiles = File.Exists(path) ? Read<List<LegacyModelProfile>>(path) ?? [] : [];
        if (profiles.Count == 0 && (!string.IsNullOrWhiteSpace(fallbackEndpoint) || !string.IsNullOrWhiteSpace(fallbackModel)))
        {
            profiles.Add(new() { Id = Guid.NewGuid(), Name = "Default", Endpoint = fallbackEndpoint, Model = fallbackModel });
        }
        foreach (LegacyModelProfile legacy in profiles)
        {
            ModelProviderProfile provider = ConvertProfile(legacy);
            settings.ModelProviderProfiles.Add(provider);
        }
        settings.ModelProviderProfiles.MoveCurrentToFirst();
    }

    public static ModelProviderProfile ConvertProfile(LegacyModelProfile legacy)
    {
        ModelProviderType providerType = legacy.Provider switch
        {
            1 => ModelProviderType.OpenAIResponses,
            2 => ModelProviderType.Anthropic,
            _ => ModelProviderType.OpenAIChatCompletion,
        };
        ModelProfile model = new() { Id = legacy.Id, Name = legacy.Model, ModelId = legacy.Model };
        model.AgentOptions.MaxOutputTokens = legacy.MaxOutputTokens;
        model.AgentOptions.Temperature = legacy.Temperature.HasValue ? (float)legacy.Temperature.Value : null;
        model.AgentOptions.ReasoningEffort = legacy.ReasoningEffort switch
        {
            "low" => EnumBox.Of(ReasoningEffort.Low),
            "medium" => EnumBox.Of(ReasoningEffort.Medium),
            "high" => EnumBox.Of(ReasoningEffort.High),
            _ => null,
        };
        ModelProviderProfile provider = new()
        {
            Id = legacy.Id,
            Name = legacy.Name,
            ProviderType = EnumBox.Of(providerType),
            Endpoint = NormalizeEndpoint(legacy.Endpoint, providerType),
        };
        provider.ModelProfiles.Add(model);
        provider.ModelProfiles.CurrentItem = model;
        return provider;
    }

    public static AgentConversation ConvertConversation(LegacyConversation legacy, AgentSettings settings)
    {
        ModelProviderProfile? provider = settings.ModelProviderProfiles.FirstOrDefault(item => item.Id == legacy.ProfileId);
        AgentConversation conversation = new()
        {
            Id = legacy.Id,
            Title = legacy.Title,
            CreatedAt = legacy.UpdatedAt,
            UpdatedAt = legacy.UpdatedAt,
            InputText = legacy.InputText,
            AccountSnapshot = legacy.Snapshot,
            ModelProviderProfileId = provider?.Id,
            ModelProfileId = provider?.ModelProfiles.CurrentItem?.Id,
            SeedMessages = [],
        };
        foreach (LegacyChatMessage old in legacy.Messages)
        {
            if (old.Role is not ("user" or "assistant"))
            {
                throw new JsonException("Unknown legacy message role");
            }
            ChatRole role = old.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;
            ObservableChatMessage message = ObservableChatMessage.Create(role, legacy.UpdatedAt);
            if (!string.IsNullOrEmpty(old.Reasoning))
            {
                message.Contents.AddOrUpdate(ObservableTextReasoningContent.Create(new TextReasoningContent(old.Reasoning)));
            }
            if (!string.IsNullOrEmpty(old.Content))
            {
                message.Contents.AddOrUpdate(ObservableTextContent.Create(new TextContent(old.Content)));
            }
            if (!string.IsNullOrEmpty(old.Error))
            {
                message.Contents.AddOrUpdate(ObservableErrorContent.Create(old.Error));
            }
            if (old.InputTokens.HasValue || old.OutputTokens.HasValue)
            {
                message.Contents.AddOrUpdate(new ObservableUsageContent
                {
                    InputTokenCount = old.InputTokens,
                    OutputTokenCount = old.OutputTokens,
                    ReasoningTokenCount = old.ReasoningTokens,
                    CachedInputTokenCount = old.CachedInputTokens,
                    TotalTokenCount = (old.InputTokens ?? 0) + (old.OutputTokens ?? 0),
                });
            }
            conversation.Messages.Add(message);
            if (role == ChatRole.User || old.Completed)
            {
                conversation.SeedMessages.Add(new ChatMessage(role, old.Content));
            }
        }
        return conversation;
    }

    public static T? Read<T>(string path)
    {
        using FileStream stream = File.OpenRead(path);
        if (stream.Length > 33554432)
        {
            throw new InvalidDataException("Legacy account insights data exceeds the size limit");
        }
        return JsonSerializer.Deserialize<T>(stream);
    }

    private static string NormalizeEndpoint(string endpoint, ModelProviderType provider)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
        {
            return endpoint;
        }
        UriBuilder builder = new(uri);
        string path = builder.Path.TrimEnd('/');
        string[] suffixes = provider == ModelProviderType.Anthropic ? ["/v1/messages", "/messages"] : ["/chat/completions", "/responses"];
        foreach (string suffix in suffixes)
        {
            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                path = path[..^suffix.Length];
                break;
            }
        }
        builder.Path = path;
        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }
}
