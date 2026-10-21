// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO.Ini;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Game.Configuration;

// Call only after a user-requested full verification/repair succeeds.
public static class GameInstallationMetadataRepair
{
    public static void Commit(string configPath, string scriptPath, Version verifiedVersion)
    {
        List<IniElement> config = IniSerializer.DeserializeFromFile(configPath).ToList();
        string version = verifiedVersion.ToString();
        int index = config.FindIndex(static item => item is IniParameter p && string.Equals(p.Key, "game_version", StringComparison.OrdinalIgnoreCase));
        if (index >= 0) { config[index] = new IniParameter("game_version", version); }
        else
        {
            int section = config.FindIndex(static item => item is IniSection s && string.Equals(s.Name, "general", StringComparison.OrdinalIgnoreCase));
            if (section < 0) { config.Add(new IniSection("general", ImmutableArray<IniElement>.Empty)); section = config.Count - 1; }
            config.Insert(section + 1, new IniParameter("game_version", version));
        }

        string suffix = ".hutao-version-" + Guid.NewGuid().ToString("N");
        string configBackup = configPath + suffix + ".bak";
        string scriptBackup = scriptPath + suffix + ".bak";
        string configTemp = configPath + suffix + ".tmp";
        string scriptTemp = scriptPath + suffix + ".tmp";
        bool hadScript;
        try { File.GetAttributes(scriptPath); hadScript = true; }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) { hadScript = false; }
        bool configChanged = false;
        bool scriptChanged = false;
        // Back up both sources before replacing either. Never reconstruct one
        // source from the other merely because its version number is higher.
        File.Copy(configPath, configBackup);
        if (hadScript) { File.Copy(scriptPath, scriptBackup); }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
            IniSerializer.SerializeToFile(configTemp, config);
            File.WriteAllText(scriptTemp, version);
            File.Move(configTemp, configPath, true);
            configChanged = true;
            File.Move(scriptTemp, scriptPath, true);
            scriptChanged = true;
        }
        catch (Exception original)
        {
            try
            {
                if (configChanged) { File.Copy(configBackup, configPath, true); }
                if (scriptChanged)
                {
                    if (hadScript) { File.Copy(scriptBackup, scriptPath, true); }
                    else { File.Delete(scriptPath); }
                }
            }
            catch (Exception rollback) { throw new AggregateException("Could not restore the backed-up game version metadata", original, rollback); }
            throw;
        }
        finally
        {
            foreach (string temporary in new[] { configTemp, scriptTemp })
            {
                try { File.Delete(temporary); }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
                {
                    System.Diagnostics.Trace.TraceWarning("Could not remove a temporary game metadata file: {0}", cleanup);
                }
            }
        }
    }
}
