namespace Esolang.Funge.Processor.Tests;

public class StackStackTests
{
    [Test]
    public async Task PushPop_MaintainsLIFO()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.Push(2);
        await Assert.That(ss.Pop()).IsEqualTo(2);
        await Assert.That(ss.Pop()).IsEqualTo(1);
        await Assert.That(ss.Pop()).IsEqualTo(0); // Empty returns 0
    }

    [Test]
    public async Task StackStackOperations_ManageStacksCorrectly()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.PushNewStack();
        ss.Push(2);

        await Assert.That(ss.TOSS.Peek()).IsEqualTo(2);
        await Assert.That(ss.HasSOSS).IsTrue();
        await Assert.That(ss.StackCount).IsEqualTo(2);

        ss.PopCurrentStack();
        await Assert.That(ss.TOSS.Peek()).IsEqualTo(1);
        await Assert.That(ss.HasSOSS).IsFalse();
        await Assert.That(ss.StackCount).IsEqualTo(1);
    }

    [Test]
    public async Task Clone_CreatesDeepCopy()
    {
        var ss = new StackStack();
        ss.Push(1);
        ss.PushNewStack();
        ss.Push(2);

        var clone = ss.Clone();
        await Assert.That(clone.Pop()).IsEqualTo(2);

        // Ensure original is unaffected
        await Assert.That(ss.TOSS.Peek()).IsEqualTo(2);
    }
}
