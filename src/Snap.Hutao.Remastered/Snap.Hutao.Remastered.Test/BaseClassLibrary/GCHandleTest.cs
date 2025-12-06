using System.IO;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Test.BaseClassLibrary;

[TestClass]
public class GCHandleTest
{
    [TestMethod]
    public unsafe void TypedGCHandleIsSameSizeAsNInt()
    {
        Assert.AreEqual(sizeof(GCHandle<Stream>), sizeof(nint));
    }
}