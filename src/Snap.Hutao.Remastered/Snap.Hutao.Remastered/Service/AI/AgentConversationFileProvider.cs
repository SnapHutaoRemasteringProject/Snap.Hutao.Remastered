// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Core.AI.IO;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Migration;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI;

[Service(ServiceLifetime.Scoped, typeof(IAgentConversationProvider))]
internal sealed class AgentConversationFileProvider(IServiceProvider serviceProvider) : IAgentConversationProvider
{
    private readonly AgentAccountContext accountContext = serviceProvider.GetRequiredService<AgentAccountContext>();

    private string DirectoryFullPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(accountContext.AccountKey) || accountContext.AccountKey.Any(static value => !char.IsAsciiDigit(value)))
            {
                throw new InvalidOperationException("An account must be selected before accessing agent conversations.");
            }
            return Path.Combine(WellKnownLocations.Settings, "Accounts", accountContext.AccountKey);
        }
    }

    private readonly JsonSerializerOptions jsonOptions = serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.AgentConversation);

    public IReadOnlyList<AgentConversation> LoadConversations()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.AgentConversationLoad, "Load agent conversation");

        ImportLegacyConversations();

        if (!Directory.Exists(DirectoryFullPath))
        {
            return [];
        }

        List<AgentConversation> conversations = [];
        foreach (string filePath in Directory.EnumerateFiles(DirectoryFullPath, "*.json"))
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    if (JsonSerializer.Deserialize<AgentConversation>(stream, jsonOptions) is { } conversation)
                    {
                        conversations.Add(conversation);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                span.SetData(SentryData.AgentConversationFile, filePath);
                SentryDiagnostics.CaptureException(ex, span, SentryOperations.AgentConversationLoad);
            }
        }

        return conversations;
    }

    public void SaveConversation(AgentConversation conversation)
    {
        Directory.CreateDirectory(DirectoryFullPath);

        string filePath = GetConversationFilePath(conversation.Id);
        string tempFilePath = $"{filePath}.tmp";

        using (FileStream stream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, conversation, jsonOptions);
        }

        File.Move(tempFilePath, filePath, true);
    }

    public void DeleteConversation(Guid id)
    {
        string filePath = GetConversationFilePath(id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetConversationFilePath(Guid id)
    {
        return Path.Combine(DirectoryFullPath, $"{id:N}.json");
    }

    private void ImportLegacyConversations()
    {
        string marker = Path.Combine(DirectoryFullPath, ".legacy-imported");
        if (File.Exists(marker)) { return; }
        string legacyFile = Path.GetFullPath(Path.Combine(WellKnownLocations.Settings, "..", "account-" + accountContext.AccountKey + ".json"));
        if (File.Exists(legacyFile))
        {
            AgentSettings settings = serviceProvider.GetRequiredService<ISettingsProvider<AgentSettings>>().CurrentValue;
            foreach (LegacyConversation legacy in AgentLegacyMigration.Read<List<LegacyConversation>>(legacyFile) ?? [])
            {
                if (!File.Exists(GetConversationFilePath(legacy.Id)))
                {
                    SaveConversation(AgentLegacyMigration.ConvertConversation(legacy, settings));
                }
            }
        }
        Directory.CreateDirectory(DirectoryFullPath);
        File.WriteAllText(marker, "1");
    }
}
