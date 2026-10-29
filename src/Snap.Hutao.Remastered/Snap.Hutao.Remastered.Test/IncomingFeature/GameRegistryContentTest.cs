using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Snap.Hutao.Remastered.Test.IncomingFeature;

[TestClass]
public class GameRegistryContentTest
{
    private static readonly JsonSerializerOptions RegistryContentSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [TestMethod]
    [SupportedOSPlatform("windows")]
    public void GetRegistryContent()
    {
        bool chineseFound = TestGetRegistryContent(@"Software\miHoYo\原神");
        bool overseaFound = TestGetRegistryContent(@"Software\miHoYo\Genshin Impact");
        if (!chineseFound && !overseaFound)
        {
            Assert.Inconclusive("Requires an installed and registered Genshin Impact or 原神 instance; none is available on this test machine.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TestGetRegistryContent(string subkey)
    {
        using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
        using (RegistryKey? gameKey = key.OpenSubKey(subkey))
        {
            if (gameKey is null)
            {
                return false;
            }

            Dictionary<string, object> data = [];
            foreach (string valueName in gameKey.GetValueNames())
            {
                data[valueName] = gameKey.GetValueKind(valueName) switch
                {
                    RegistryValueKind.DWord => (int)gameKey.GetValue(valueName)!,
                    RegistryValueKind.Binary => GetStringOrObject((byte[])gameKey.GetValue(valueName)!),
                    RegistryValueKind.String => (string)gameKey.GetValue(valueName)!,
                    _ => throw new ArgumentException($"Unsupported type: {gameKey.GetValueKind(valueName)}"),
                };
            }

            Console.WriteLine($"Subkey: {subkey}");
            Console.WriteLine(JsonSerializer.Serialize(data, RegistryContentSerializerOptions));
            return true;
        }
    }

    private static unsafe object GetStringOrObject(byte[] bytes)
    {
        fixed (byte* pByte = bytes)
        {
            ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pByte);
            string temp = Encoding.UTF8.GetString(span);

            if (temp.AsSpan()[0] is '{' or '[')
            {
                return JsonSerializer.Deserialize<JsonElement>(temp);
            }

            return temp;
        }
    }
}