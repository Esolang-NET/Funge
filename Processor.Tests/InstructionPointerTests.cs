using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor.Tests;

public class InstructionPointerTests
{
    [Test]
    public async Task CreateChild_CopiesStateCorrectly()
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

        await Assert.That(child.Id).IsEqualTo(2);
        await Assert.That(child.Position).IsEqualTo(parent.Position);
        await Assert.That(child.Delta).IsNotEqualTo(parent.Delta); // Should be reflected
        await Assert.That(child.Offset).IsEqualTo(parent.Offset);
        await Assert.That(child.StringMode).IsEqualTo(parent.StringMode);

        // Stack should be cloned
        await Assert.That(child.StackStack.Pop()).IsEqualTo(42);
        child.StackStack.Push(99);
        await Assert.That(parent.StackStack.Pop()).IsEqualTo(42); // Original unaffected
    }
}
