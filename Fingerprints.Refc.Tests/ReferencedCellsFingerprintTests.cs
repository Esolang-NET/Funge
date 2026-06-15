namespace Esolang.Funge.Fingerprints.Refc;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
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
}

sealed class NoVectorContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class ReferencedCellsFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ReferencedCellsFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ReferencedCellsFingerprint().Handprint).IsEqualTo(0x52454643);

    [Test]
    public async Task Instruction_R_D_RoundTrip()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();

        // Push vector (x=1, y=2, z=3)
        ctx.Push(1); ctx.Push(2); ctx.Push(3);
        (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var refId = ctx.Pop();

        // Dereference
        ctx.Push(refId);
        (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();

        var z = ctx.Pop();
        var y = ctx.Pop();
        var x = ctx.Pop();
        await Assert.That(x).IsEqualTo(1);
        await Assert.That(y).IsEqualTo(2);
        await Assert.That(z).IsEqualTo(3);
    }

    [Test]
    public async Task Instruction_D_InvalidRef_Reflects()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();
        ctx.Push(999);
        (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_R_NoVector_Reflects()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new NoVectorContext();
        (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_R_AutoIncrementsRef()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();

        ctx.Push(1); ctx.Push(0); ctx.Push(0);
        (await Instruction(fp, 'R'))(ctx);
        var ref1 = ctx.Pop();

        ctx.Push(2); ctx.Push(0); ctx.Push(0);
        (await Instruction(fp, 'R'))(ctx);
        var ref2 = ctx.Pop();

        await Assert.That(ref2).IsNotEqualTo(ref1);
    }
}
