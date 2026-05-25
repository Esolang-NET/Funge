using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Processor.Tests;

[TestClass]
public class StackStackTests
{
    [TestMethod]
    public void PushPop_MaintainsLIFO()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.Push(2);
        Assert.AreEqual(2, ss.Pop());
        Assert.AreEqual(1, ss.Pop());
        Assert.AreEqual(0, ss.Pop()); // Empty returns 0
    }

    [TestMethod]
    public void StackStackOperations_ManageStacksCorrectly()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.PushNewStack();
        ss.Push(2);

        Assert.AreEqual(2, ss.TOSS.Peek());
        Assert.IsTrue(ss.HasSOSS);
        Assert.AreEqual(2, ss.StackCount);

        ss.PopCurrentStack();
        Assert.AreEqual(1, ss.TOSS.Peek());
        Assert.IsFalse(ss.HasSOSS);
        Assert.AreEqual(1, ss.StackCount);
    }

    [TestMethod]
    public void Clone_CreatesDeepCopy()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.PushNewStack();
        ss.Push(2);

        var clone = ss.Clone();
        Assert.AreEqual(2, clone.Pop());

        // Ensure original is unaffected
        Assert.AreEqual(2, ss.TOSS.Peek());
    }
}
