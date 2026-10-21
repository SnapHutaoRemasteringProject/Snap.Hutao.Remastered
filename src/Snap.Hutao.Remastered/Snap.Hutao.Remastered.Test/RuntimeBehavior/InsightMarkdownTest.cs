using Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class InsightMarkdownTest
{
    [TestMethod]
    public void StreamingIndentedFencesKeepCodeHeadingsInTheSameBlock()
    {
        const string unfinished = "  ```text\n# code heading\n\n";
        Assert.AreEqual(0, MarkdownBlockPartitioner.GetStablePrefixLength(unfinished));
        string finished = unfinished + "  ```\n";
        Assert.AreEqual(finished.Length, MarkdownBlockPartitioner.GetStablePrefixLength(finished));
    }

    [TestMethod]
    public void StreamingTablesWaitForTheNextBlockBeforeBecomingStable()
    {
        const string table = "| A | B |\n| --- | --- |\n| 1 | 2 |\n";
        Assert.AreEqual(0, MarkdownBlockPartitioner.GetStablePrefixLength(table));
        Assert.AreEqual(table.Length, MarkdownBlockPartitioner.GetStablePrefixLength(table + "# next section\n"));
    }
}
