using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor.Tests;

[TestClass]
public class InstructionPointerTests
{
    [TestMethod]
    public void CreateChild_CopiesStateCorrectly()
    {
        var parent = new InstructionPointer(1)
        {
            Position = new FungeVector(1, 2, 3),
            Delta = new FungeVector(0, 1, 0),
            Offset = new FungeVector(10, 10, 10),
            StringMode = true
        };
        parent.StackStack.Push(42);

        var child = parent.CreateChild(2);

        Assert.AreEqual(2, child.Id);
        Assert.AreEqual(parent.Position, child.Position);
        Assert.AreNotEqual(parent.Delta, child.Delta); // Should be reflected
        Assert.AreEqual(parent.Offset, child.Offset);
        Assert.AreEqual(parent.StringMode, child.StringMode);

        // Stack should be cloned
        Assert.AreEqual(42, child.StackStack.Pop());
        child.StackStack.Push(99);
        Assert.AreEqual(42, parent.StackStack.Pop()); // Original unaffected
    }
}
