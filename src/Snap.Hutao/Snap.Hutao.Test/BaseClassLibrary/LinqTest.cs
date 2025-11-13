using System;
using System.Collections.Generic;
using System.Linq;

namespace Snap.Hutao.Test.BaseClassLibrary;

[TestClass]
public sealed class LinqTest
{
    [TestMethod]
    public void LinqOrderByWithWrapperStructThrow()
    {
        List<MyUInt32> list = [1, 5, 2, 6, 3, 7, 4, 8];
        string result = default!;
        Assert.Throws<InvalidOperationException>(() =>
        {
            result = string.Join(", ", list.OrderBy(i => i).Select(i => i.Value));
        });

        Console.WriteLine(result);
    }

    [TestMethod]
    public void SequenceAndRangeProduceSameRusult()
    {
        IEnumerable<int> rangeResult = Enumerable.Range(4, 5);
        IEnumerable<int> sequenceResult = Enumerable.Sequence(4U, 8U, 1U).Select(x => (int)x);

        CollectionAssert.AreEqual(rangeResult.ToList(), sequenceResult.ToList());
    }

    private readonly struct MyUInt32
    {
        public readonly uint Value;

        public MyUInt32(uint value)
        {
            Value = value;
        }

        public static implicit operator MyUInt32(uint value)
        {
            return new(value);
        }
    }
}