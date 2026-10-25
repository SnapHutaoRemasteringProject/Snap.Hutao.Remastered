// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;
using System.IO;
using System.Text;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class GachaLogQueryWebCacheProviderTest
{
    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchReturnsNullWhenCacheEndsAtUrlPrefix(bool isOversea, string prefix)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(prefix));

        Assert.IsNull(GachaLogQueryWebCacheProvider.Match(stream, isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchReturnsNullWhenUrlHasNoTerminator(bool isOversea, string prefix)
    {
        Assert.IsNull(Match(prefix + "-df01aea2/index.html?authkey=SYNTHETIC", isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchStopsAtNullTerminator(bool isOversea, string prefix)
    {
        Assert.AreEqual("-df01aea2/index.html?authkey=SYNTHETIC", Match("cache bytes" + prefix + "-df01aea2/index.html?authkey=SYNTHETIC\0trailing cache bytes", isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchSelectsLatestUrl(bool isOversea, string prefix)
    {
        Assert.AreEqual("/index.html?authkey=LATEST", Match(prefix + "/index.html?authkey=OLD\0" + prefix + "/index.html?authkey=LATEST\0", isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchRejectsTruncatedLatestUrl(bool isOversea, string prefix)
    {
        Assert.IsNull(Match(prefix + "/index.html?authkey=OLD\0" + prefix + "/index.html?authkey=TRUNCATED", isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    [DataRow(true, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    public void MatchIgnoresOtherRegionUrl(bool isOversea, string prefix)
    {
        Assert.IsNull(Match(prefix + "/index.html?authkey=SYNTHETIC\0", isOversea));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MatchReturnsNullWithoutUrlPrefix(bool isOversea)
    {
        Assert.IsNull(Match("unrelated cache bytes\0", isOversea));
        Assert.IsNull(Match(string.Empty, isOversea));
    }

    [TestMethod]
    [DataRow(false, "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha")]
    [DataRow(true, "https://gs.hoyoverse.com/genshin/event/e20190909gacha")]
    public void MatchReturnsEmptySuffixWhenTerminatorFollowsPrefix(bool isOversea, string prefix)
    {
        Assert.AreEqual(string.Empty, Match(prefix + "\0", isOversea));
    }

    private static string? Match(string cache, bool isOversea)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(cache));
        return GachaLogQueryWebCacheProvider.Match(stream, isOversea);
    }
}
