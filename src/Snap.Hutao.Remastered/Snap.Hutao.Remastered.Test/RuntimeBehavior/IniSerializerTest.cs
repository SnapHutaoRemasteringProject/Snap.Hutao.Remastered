using Snap.Hutao.Remastered.Core.IO.Ini;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class IniSerializerTest
{
    [TestMethod]
    public void GlobalCommentContainingEqualsIsOnlyAComment()
    {
        ImmutableArray<IniElement> elements = Deserialize("; example=value\n");

        Assert.AreEqual(1, elements.Length);
        Assert.IsInstanceOfType<IniComment>(elements[0]);
        Assert.AreEqual("; example=value", elements[0].ToString());
    }

    [TestMethod]
    public void SectionCommentContainingEqualsIsOnlyAComment()
    {
        ImmutableArray<IniElement> elements = Deserialize("[General]\n; example=value\ngame_version=6.0.0\n");

        Assert.AreEqual(3, elements.Length);
        IniSection section = (IniSection)elements[0];
        Assert.AreEqual(2, section.Children.Length);
        Assert.IsInstanceOfType<IniComment>(section.Children[0]);
        Assert.AreEqual("game_version", ((IniParameter)section.Children[1]).Key);
    }

    [TestMethod]
    public void SectionNameContainingEqualsDoesNotCreateAParameter()
    {
        ImmutableArray<IniElement> elements = Deserialize("[name=value]\nkey=value\n");

        Assert.AreEqual(2, elements.Length);
        IniSection section = (IniSection)elements[0];
        Assert.AreEqual("name=value", section.Name);
        Assert.AreEqual(1, section.Children.Length);
        Assert.AreEqual("key", ((IniParameter)section.Children[0]).Key);
    }

    [TestMethod]
    public void RepeatedRoundTripsPreserveCommentsWithoutAddingLines()
    {
        const string input = "; global=value\n[General]\n; example=value\ngame_version=6.0.0\n";
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, input);
            for (int iteration = 0; iteration < 3; iteration++)
            {
                ImmutableArray<IniElement> elements = IniSerializer.DeserializeFromFile(path);
                IniSerializer.SerializeToFile(path, elements);

                Assert.AreEqual(input, File.ReadAllText(path).Replace("\r\n", "\n"));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ParameterValuePreservesAdditionalEqualsSigns()
    {
        ImmutableArray<IniElement> elements = Deserialize("key=value=with=equals\n");

        Assert.AreEqual(1, elements.Length);
        IniParameter parameter = (IniParameter)elements[0];
        Assert.AreEqual("key", parameter.Key);
        Assert.AreEqual("value=with=equals", parameter.Value);
    }

    private static ImmutableArray<IniElement> Deserialize(string text)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(text));
        return IniSerializer.Deserialize(stream);
    }
}
