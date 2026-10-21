// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Windows.Security.Credentials;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal static class AgentCredentialStore
{
    private const string Resource = "Snap.Hutao.Remastered.AccountInsights";

    public static void Load(AgentSettings settings)
    {
        foreach (PasswordCredential credential in FindCredentials(new PasswordVault()))
        {
            Models.ModelProviderProfile? profile = settings.ModelProviderProfiles.FirstOrDefault(profile => profile.Id.ToString("N") == credential.UserName);
            if (profile is not null)
            {
                credential.RetrievePassword();
                profile.ApiKey = credential.Password;
            }
        }
    }

    public static void Save(AgentSettings settings)
    {
        PasswordVault vault = new();
        Dictionary<string, PasswordCredential> existing = FindCredentials(vault).ToDictionary(static credential => credential.UserName, StringComparer.Ordinal);
        foreach (Models.ModelProviderProfile profile in settings.ModelProviderProfiles)
        {
            string id = profile.Id.ToString("N");
            existing.Remove(id, out PasswordCredential? credential);
            if (string.IsNullOrEmpty(profile.ApiKey))
            {
                if (credential is not null) { vault.Remove(credential); }
                continue;
            }
            if (credential is not null)
            {
                credential.RetrievePassword();
                if (credential.Password == profile.ApiKey) { continue; }
            }
            vault.Add(new PasswordCredential(Resource, id, profile.ApiKey));
        }
        foreach (PasswordCredential credential in existing.Values)
        {
            vault.Remove(credential);
        }
    }

    private static IReadOnlyList<PasswordCredential> FindCredentials(PasswordVault vault)
    {
        try { return vault.FindAllByResource(Resource); }
        catch (Exception exception) when (exception.HResult == unchecked((int)0x80070490)) { return []; }
    }
}
