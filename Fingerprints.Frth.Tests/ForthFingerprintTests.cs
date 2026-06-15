using TUnit.Assertions.Enums;
namespace Esolang.Funge.Fingerprints.Frth;

sealed class TestContext : IFungeExecutionContext, IFungeStackContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; private set; }

    public int StackDepth => _stack.Count;

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;
}

public class ForthFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ForthFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
        return handler;
    }

    static TestContext CreateContext(params int[] values)
    {
        var context = new TestContext();
        foreach (var value in values)
            context.Push(value);
        return context;
    }

    static int[] PopAll(TestContext context)
    {
        List<int> values = [];
        while (context.StackDepth > 0)
            values.Add(context.Pop());

        values.Reverse();
        return [.. values];
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ForthFingerprint().Handprint).IsEqualTo(0x46525448);

    [Test]
    public async Task D_PushesStackDepth()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3);

        (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3, 3], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task O_PerformsForthOver()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2);

        (await Instruction(fingerprint, 'O'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 1], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task R_PerformsForthRot()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3);

        (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[2, 3, 1], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_PicksIndexedValue()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 0);

        (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3, 3], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_PushesZeroWhenIndexIsTooDeep()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 5);

        (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3, 0], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_ReflectsOnNegativeIndex()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, -1);

        (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task L_RollsIndexedValueToTop()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, 3);

        (await Instruction(fingerprint, 'L'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 3, 4, 5, 2], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task L_WithPositiveIndexPastDepth_PushesZero()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 5);

        (await Instruction(fingerprint, 'L'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3, 0], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task L_WithNegativeIndexMovesTopDeeper()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, -3);

        (await Instruction(fingerprint, 'L'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 5, 3, 4], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task L_WithNegativeIndexBeyondDepth_PadsWithZeroes()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, -5);

        (await Instruction(fingerprint, 'L'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[3, 0, 0, 1, 2], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }
}
