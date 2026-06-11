namespace Esolang.Funge.Fingerprints.Toys;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext, IFungePositionContext, IFungeStackContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public int StackDepth => _stack.Count;
    public int Dimensions => 2;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) Position { get; set; } = (5, 5, 0);
    public (int X, int Y, int Z) Delta { get; set; } = (1, 0, 0);

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop(); var y = Pop(); var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z) { Push(x); Push(y); Push(z); }
}

sealed class BasicContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class ToysFingerprintTests
{
    static FingerprintInstruction Instruction(ToysFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x544F5953, new ToysFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_Replicate()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(42); // value
        ctx.Push(3);  // n
        Instruction(fp, 'A')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(42, ctx.Pop());
        Assert.AreEqual(42, ctx.Pop());
        Assert.AreEqual(42, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_B_Butterfly()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // a
        ctx.Push(5); // b
        Instruction(fp, 'B')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(-2, ctx.Pop()); // a-b
        Assert.AreEqual(8, ctx.Pop());  // a+b
    }

    [TestMethod]
    public void Instruction_D_Decrement()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(10);
        Instruction(fp, 'D')(ctx);
        Assert.AreEqual(9, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_I_Increment()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(9);
        Instruction(fp, 'I')(ctx);
        Assert.AreEqual(10, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_N_Negate()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(5);
        Instruction(fp, 'N')(ctx);
        Assert.AreEqual(-5, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_H_Shift()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(4); // a
        ctx.Push(2); // b
        Instruction(fp, 'H')(ctx);
        Assert.AreEqual(16, ctx.Pop()); // 4 << 2
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_E_Sum()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(1); ctx.Push(2); ctx.Push(3);
        Instruction(fp, 'E')(ctx);
        Assert.AreEqual(6, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_P_Product()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(2); ctx.Push(3); ctx.Push(4);
        Instruction(fp, 'P')(ctx);
        Assert.AreEqual(24, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_X_IncrementX()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (3, 4, 0) };
        Instruction(fp, 'X')(ctx);
        Assert.AreEqual((4, 4, 0), ctx.Position);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Y_IncrementY()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (3, 4, 0) };
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual((3, 5, 0), ctx.Position);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_R_PeekRight()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.SetCell(6, 5, 0, 99);
        Instruction(fp, 'R')(ctx);
        Assert.AreEqual(99, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_L_PeekLeft()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.SetCell(4, 5, 0, 77);
        Instruction(fp, 'L')(ctx);
        Assert.AreEqual(77, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Q_SetPrevious()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(42);
        Instruction(fp, 'Q')(ctx);
        Assert.AreEqual(42, ctx.GetCell(4, 5, 0));
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_S_FillSpace()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(7); // value
        ctx.Push(3); ctx.Push(2); ctx.Push(0); // size (3,2)
        ctx.Push(0); ctx.Push(0); ctx.Push(0); // dest (0,0,0)
        Instruction(fp, 'S')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(7, ctx.GetCell(0, 0, 0));
        Assert.AreEqual(7, ctx.GetCell(2, 1, 0));
    }

    [TestMethod]
    public void Instruction_E_NoStackContext_Reflects()
    {
        var fp = new ToysFingerprint();
        var ctx = new BasicContext();
        Instruction(fp, 'E')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
