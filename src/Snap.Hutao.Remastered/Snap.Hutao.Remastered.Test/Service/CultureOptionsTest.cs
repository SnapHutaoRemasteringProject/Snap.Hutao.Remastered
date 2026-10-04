using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using System.Globalization;

namespace Snap.Hutao.Remastered.Test.Service;

[TestClass]
[DoNotParallelize]
public sealed class CultureOptionsTest
{
    [TestMethod]
    [DataRow("en-US", "zh-CN", "en")]
    [DataRow("zh-TW", "en-US", "zh-Hant")]
    [DataRow("nl-NL", "en-US", "zh-Hans")]
    public void UnsetPreferenceUsesDisplayLanguage(string displayLanguage, string regionalFormat, string expected)
    {
        VerifyCurrentCulture(new Dictionary<string, string>(), displayLanguage, regionalFormat, expected);
    }

    [TestMethod]
    [DataRow("en-US", "en")]
    [DataRow("zh-TW", "zh-Hant")]
    [DataRow("nl-NL", "zh-Hans")]
    public void SavedPreferenceOverridesDisplayLanguageAndNormalizes(string savedLanguage, string expected)
    {
        Dictionary<string, string> settings = new() { [SettingKeys.PrimaryLanguage] = savedLanguage };
        VerifyCurrentCulture(settings, "ja-JP", "de-DE", expected);
    }

    private static void VerifyCurrentCulture(IReadOnlyDictionary<string, string> settings,
        string displayLanguage, string regionalFormat, string expected)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(regionalFormat);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(displayLanguage);
            CultureOptions options = new(settings);

            Assert.AreEqual(expected, options.CurrentCulture.Value.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }
    }
}
