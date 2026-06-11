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

[TestClass]
public class ReferencedCellsFingerprintTests
{
    static FingerprintInstruction Instruction(ReferencedCellsFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x52454643, new ReferencedCellsFingerprint().Handprint);

    [TestMethod]
    public void Instruction_R_D_RoundTrip()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();

        // Push vector (x=1, y=2, z=3)
        ctx.Push(1); ctx.Push(2); ctx.Push(3);
        Instruction(fp, 'R')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var refId = ctx.Pop();

        // Dereference
        ctx.Push(refId);
        Instruction(fp, 'D')(ctx);
        Assert.IsFalse(ctx.Reflected);

        var z = ctx.Pop();
        var y = ctx.Pop();
        var x = ctx.Pop();
        Assert.AreEqual(1, x);
        Assert.AreEqual(2, y);
        Assert.AreEqual(3, z);
    }

    [TestMethod]
    public void Instruction_D_InvalidRef_Reflects()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();
        ctx.Push(999);
        Instruction(fp, 'D')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_R_NoVector_Reflects()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new NoVectorContext();
        Instruction(fp, 'R')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_R_AutoIncrementsRef()
    {
        var fp = new ReferencedCellsFingerprint();
        var ctx = new TestContext();

        ctx.Push(1); ctx.Push(0); ctx.Push(0);
        Instruction(fp, 'R')(ctx);
        var ref1 = ctx.Pop();

        ctx.Push(2); ctx.Push(0); ctx.Push(0);
        Instruction(fp, 'R')(ctx);
        var ref2 = ctx.Pop();

        Assert.AreNotEqual(ref1, ref2);
    }
}
