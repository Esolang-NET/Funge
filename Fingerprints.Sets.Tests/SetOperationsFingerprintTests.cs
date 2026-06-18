using TUnit.Assertions.Enums;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Sets;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class SetOperationsFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(SetOperationsFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void PushSet(TestContext ctx, params int[] elements)
    {
        foreach (var e in elements)
            ctx.Push(e);
        ctx.Push(elements.Length);
    }

    static HashSet<int> PopSet(TestContext ctx)
    {
        var count = ctx.Pop();
        var set = new HashSet<int>();
        for (var i = 0; i < count; i++)
            set.Add(ctx.Pop());
        return set;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new SetOperationsFingerprint().Handprint).IsEqualTo(0x53455453);

    [Test]
    public async Task Instruction_A_AddElement()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        ctx.Push(4);
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[1, 2, 3, 4], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_A_AddExisting_NoChange()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        ctx.Push(2);
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[1, 2], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_U_Union()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        PushSet(ctx, 2, 3);
        await (await Instruction(fp, 'U'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[1, 2, 3], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_I_Intersect()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        PushSet(ctx, 2, 3, 4);
        await (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[2, 3], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_S_Subtract()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        PushSet(ctx, 2, 3);
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[1], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_R_Remove()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        ctx.Push(2);
        await (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = PopSet(ctx);
        await Assert.That(result).IsEquivalentTo((int[])[1, 3], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_M_Member_Found()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        ctx.Push(2);
        PushSet(ctx, 1, 2, 3);
        await (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(1);
    }

    [Test]
    public async Task Instruction_M_Member_NotFound()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        ctx.Push(99);
        PushSet(ctx, 1, 2, 3);
        await (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(0);
    }

    [Test]
    public async Task Instruction_Z_Discard()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        await (await Instruction(fp, 'Z'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(0); // stack empty
    }

    [Test]
    public async Task Instruction_D_Duplicate()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 5, 6);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var first = PopSet(ctx);
        var second = PopSet(ctx);
        await Assert.That(first).IsEquivalentTo((int[])[5, 6], CollectionOrdering.Any);
        await Assert.That(second).IsEquivalentTo((int[])[5, 6], CollectionOrdering.Any);
    }

    [Test]
    public async Task Instruction_X_Exchange()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        PushSet(ctx, 3, 4);
        await (await Instruction(fp, 'X'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var top = PopSet(ctx);
        var bottom = PopSet(ctx);
        await Assert.That(top).IsEquivalentTo((int[])[1, 2], CollectionOrdering.Any);
        await Assert.That(bottom).IsEquivalentTo((int[])[3, 4], CollectionOrdering.Any);
    }
}
