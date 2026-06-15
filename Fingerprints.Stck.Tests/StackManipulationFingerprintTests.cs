using System.Text;
using TUnit.Assertions.Enums;

namespace Esolang.Funge.Fingerprints.Stck;

sealed class TestContext : IFungeExecutionContext, IFungeStackContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _space = [];
    readonly StringBuilder _output = new();

    public bool Reflected { get; private set; }

    public int StackDepth => _stack.Count;

    public (int X, int Y, int Z) StorageOffset { get; set; }

    public int Dimensions => 3;

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop();
        var y = Pop();
        var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z)
    {
        Push(x);
        Push(y);
        Push(z);
    }

    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var value) ? value : 32;

    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public void WriteString(string value) => _output.Append(value);

    public string Output => _output.ToString();
}

public class StackManipulationFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(StackManipulationFingerprint fingerprint, char instruction)
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
        => await Assert.That(new StackManipulationFingerprint().Handprint).IsEqualTo(0x5354434B);

    [Test]
    public async Task B_BuriesValueAtRequestedDepth()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 99, 2);

        (await Instruction(fingerprint, 'B'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 99, 2, 3], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task B_WithNegativeDepth_PushesZeroesAboveValue()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(7, 42, -2);

        (await Instruction(fingerprint, 'B'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[7, 42, 0, 0], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task B_ReflectsWhenRequestedDepthIsTooDeep()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 99, 3);

        (await Instruction(fingerprint, 'B'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task C_PushesCurrentStackDepth()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        (await Instruction(fingerprint, 'C'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task D_DuplicatesTopNValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 3);

        (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 1, 2, 2, 3, 3], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task G_ReproducesValuesWrittenByW()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(10, 20, 30);
        context.StorageOffset = (10, 0, 0);
        context.Push(3);
        context.PushVector(1, 0, 0);
        context.PushVector(1, 0, 0);

        (await Instruction(fingerprint, 'W'))(context);

        await Assert.That(context.GetCell(11, 0, 0)).IsEqualTo(30);
        await Assert.That(context.GetCell(12, 0, 0)).IsEqualTo(20);
        await Assert.That(context.GetCell(13, 0, 0)).IsEqualTo(10);

        context.Push(3);
        context.PushVector(1, 0, 0);
        context.PushVector(1, 0, 0);

        (await Instruction(fingerprint, 'G'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[10, 20, 30], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task K_MovesRequestedBlockToTop()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, 3, 1);

        (await Instruction(fingerprint, 'K'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 5, 2, 3, 4], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task K_ReflectsWhenEndIsDeeperThanStart()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 1, 2);

        (await Instruction(fingerprint, 'K'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task N_ReversesTopNValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 3);

        (await Instruction(fingerprint, 'N'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 4, 3, 2], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_PrintsBottomToTopNonDestructively()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(context.Output).IsEqualTo("1 2 3 ");
        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 3], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task R_ReversesEntireStack()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4);

        (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[4, 3, 2, 1], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task S_DuplicatesSecondValue()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2);

        (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 1, 2], CollectionOrdering.Matching);
    }

    [Test]
    public async Task T_SwapsSecondAndThirdValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        (await Instruction(fingerprint, 'T'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[2, 1, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task U_DropsUntilValueIsFound()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 2);

        (await Instruction(fingerprint, 'U'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[1, 2, 2], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task U_ReflectsWhenValueIsNotFound()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 9);

        (await Instruction(fingerprint, 'U'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task W_ReflectsOnNegativeCount()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, -1);
        context.PushVector(1, 0, 0);
        context.PushVector(0, 0, 0);

        (await Instruction(fingerprint, 'W'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task Z_ConvertsZeroStringToZeroGnirts()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext('a', 'b', 'c', 0);

        (await Instruction(fingerprint, 'Z'))(context);

        await Assert.That(PopAll(context)).IsEquivalentTo((int[])[0, 'a', 'b', 'c'], CollectionOrdering.Matching);
        await Assert.That(context.Reflected).IsFalse();
    }
}
