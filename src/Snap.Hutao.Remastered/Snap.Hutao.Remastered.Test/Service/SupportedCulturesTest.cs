using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using System.Globalization;
using System.Linq;

namespace Snap.Hutao.Remastered.Test.Service;

[TestClass]
public sealed class SupportedCulturesTest
{
    [TestMethod]
    [DataRow("en-US", "en")]
    [DataRow("en-GB", "en")]
    [DataRow("de-DE", "de")]
    [DataRow("pt-BR", "pt")]
    [DataRow("ja-JP", "ja")]
    [DataRow("zh-CN", "zh-Hans")]
    [DataRow("zh-SG", "zh-Hans")]
    [DataRow("zh-TW", "zh-Hant")]
    [DataRow("zh-HK", "zh-Hant")]
    [DataRow("zh-Hans", "zh-Hans")]
    [DataRow("zh-Hant", "zh-Hant")]
    [DataRow("nl-NL", "zh-Hans")]
    [DataRow("", "zh-Hans")]
    public void ResolvesToSupportedLanguage(string language, string expected)
    {
        CultureInfo result = SupportedCultures.GetSupportedCulture(CultureInfo.GetCultureInfo(language));

        Assert.AreEqual(expected, result.Name);
        Assert.IsTrue(SupportedCultures.GetValues().Any(option => option.Value.Equals(result)));
    }

    [TestMethod]
    public void PreservesAllSupportedLanguages()
    {
        foreach (NameCultureInfoValue option in SupportedCultures.GetValues())
        {
            Assert.AreEqual(option.Value, SupportedCultures.GetSupportedCulture(option.Value));
        }
    }
}
