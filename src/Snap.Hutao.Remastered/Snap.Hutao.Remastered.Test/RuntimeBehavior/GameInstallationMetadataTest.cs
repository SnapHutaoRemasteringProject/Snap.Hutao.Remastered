using Snap.Hutao.Remastered.Service.Game.Configuration;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.IO;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class GameInstallationMetadataTest
{
    [TestMethod]
    public void InspectionKeepsChannelsIndependentAndDoesNotCreateMissingVersionFile()
    {
        WithInstallation((exe, config, script) =>
        {
            File.WriteAllText(config, "[general]\nchannel=14\nsub_channel=1\ngame_version=6.1.0\n");
            GameInstallationInfo info = GameInstallationReader.Read(exe, config, script, false);
            Assert.AreEqual(ChannelType.Bili, info.Channel.Channel);
            Assert.AreEqual(new Version(6, 1, 0), info.InstalledVersion);
            Assert.AreEqual(GameVersionFileStatus.Missing, info.ScriptStatus);
            Assert.IsFalse(File.Exists(script));
        });
    }

    [TestMethod]
    public void ConflictingSourcesNeverSelectTheHigherVersion()
    {
        WithInstallation((exe, config, script) =>
        {
            File.WriteAllText(config, "[general]\nchannel=1\nsub_channel=1\ngame_version=6.2.0\n");
            File.WriteAllText(script, "6.1.0");
            GameInstallationInfo info = GameInstallationReader.Read(exe, config, script, true);
            Assert.IsTrue(info.HasVersionConflict);
            Assert.IsNull(info.InstalledVersion);
            Assert.AreEqual(GameVersionFileStatus.Valid, info.ConfigStatus);
            Assert.AreEqual(ChannelOptionsErrorKind.None, info.Channel.ErrorKind);
            Assert.IsTrue(info.IsOversea);
        });
    }

    [TestMethod]
    public void RefreshReadsExternalUpdatesAndClearsADeletedOrInvalidVersion()
    {
        WithInstallation((exe, config, script) =>
        {
            File.WriteAllText(config, "game_version=6.0.0");
            File.WriteAllText(script, "6.0.0");
            GameInstallationInfo first = GameInstallationReader.Read(exe, config, script, false);
            File.WriteAllText(config, "game_version=6.1.0");
            File.WriteAllText(script, "6.1.0");
            GameInstallationInfo second = GameInstallationReader.Read(exe, config, script, false);
            Assert.AreEqual(new Version(6, 1, 0), second.InstalledVersion);
            Assert.IsFalse(first.MatchesVersionFiles(second));
            File.Delete(config);
            File.WriteAllText(script, "not a version");
            GameInstallationInfo invalid = GameInstallationReader.Read(exe, config, script, false);
            Assert.IsNull(invalid.InstalledVersion);
            Assert.AreEqual(GameVersionFileStatus.Invalid, invalid.ScriptStatus);
            Assert.IsFalse(File.Exists(config));
        });
    }

    [TestMethod]
    public void UnreadableSourceCannotBeIgnoredInFavourOfTheOtherVersion()
    {
        WithInstallation((exe, config, script) =>
        {
            File.WriteAllText(config, "game_version=6.1.0");
            File.WriteAllText(script, "6.1.0");
            using FileStream locked = File.Open(script, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            GameInstallationInfo info = GameInstallationReader.Read(exe, config, script, false);
            Assert.AreEqual(GameVersionFileStatus.Unreadable, info.ScriptStatus);
            Assert.IsNull(info.InstalledVersion);
            Assert.IsNotNull(info.ScriptReadError);
        });
    }

    [TestMethod]
    public void VerifiedRepairBacksUpBothFilesAndPreservesChannelConfiguration()
    {
        WithInstallation((exe, config, script) =>
        {
            string original = "[general]\nchannel=14\nsub_channel=1\ncps=gw_pc\ngame_version=6.0.0\n";
            File.WriteAllText(config, original);
            File.WriteAllText(script, "5.9.0");
            GameInstallationMetadataRepair.Commit(config, script, new Version(6, 1, 0));
            GameInstallationInfo info = GameInstallationReader.Read(exe, config, script, false);
            Assert.AreEqual(new Version(6, 1, 0), info.InstalledVersion);
            Assert.IsFalse(info.HasVersionProblem);
            Assert.AreEqual(ChannelType.Bili, info.Channel.Channel);
            Assert.IsTrue(File.ReadAllText(config).Contains("cps=gw_pc", StringComparison.Ordinal));
            string backup = Directory.GetFiles(Path.GetDirectoryName(config)!, "config.ini.hutao-version-*.bak").Single();
            Assert.AreEqual(original, File.ReadAllText(backup));
            Assert.HasCount(1, Directory.GetFiles(Path.GetDirectoryName(script)!, "ScriptVersion.hutao-version-*.bak"));
        });
    }

    [TestMethod]
    public void FailedSecondReplacementRestoresTheFirstFile()
    {
        WithInstallation((exe, config, script) =>
        {
            string original = "[general]\ngame_version=6.0.0\nchannel=1\n";
            File.WriteAllText(config, original);
            File.WriteAllText(script, "6.0.0");
            using (FileStream locked = File.Open(script, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Exception failure = Assert.Throws<Exception>(() => GameInstallationMetadataRepair.Commit(config, script, new Version(6, 1, 0)));
                Assert.IsTrue(failure is IOException or UnauthorizedAccessException);
                Assert.AreEqual(original, File.ReadAllText(config));
            }
            Assert.AreEqual("6.0.0", File.ReadAllText(script));
            Assert.HasCount(0, Directory.GetFiles(Path.GetDirectoryName(config)!, "*.tmp", SearchOption.AllDirectories));
        });
    }

    private static void WithInstallation(Action<string, string, string> test)
    {
        string parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "HutaoInstallationTests"));
        string directory = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        string data = Path.Combine(directory, "YuanShen_Data", "Persistent");
        Directory.CreateDirectory(data);
        try { test(Path.Combine(directory, "YuanShen.exe"), Path.Combine(directory, "config.ini"), Path.Combine(data, "ScriptVersion")); }
        finally
        {
            if (!Path.GetFullPath(directory).StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { throw new InvalidOperationException(); }
            Directory.Delete(directory, true);
        }
    }
}
