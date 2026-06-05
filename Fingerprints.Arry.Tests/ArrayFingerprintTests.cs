namespace Esolang.Funge.Fingerprints.Arry;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];

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

    public int Dimensions => 3;

    public int GetCell(int x, int y, int z) => _cells.TryGetValue((x, y, z), out var value) ? value : ' ';

    public void SetCell(int x, int y, int z, int value)
    {
        if (value == ' ')
            _cells.Remove((x, y, z));
        else
            _cells[(x, y, z)] = value;
    }
}

sealed class CoreOnlyContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class ArrayFingerprintTests
{
    static FingerprintInstruction Instruction(ArrayFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushVector(TestContext ctx, int x, int y, int z) => ctx.PushVector(x, y, z);

    static void AssertVector(TestContext ctx, int x, int y, int z)
    {
        Assert.AreEqual(z, ctx.Pop());
        Assert.AreEqual(y, ctx.Pop());
        Assert.AreEqual(x, ctx.Pop());
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x41525259, new ArrayFingerprint().Handprint);

    [TestMethod]
    public void Instruction_G_PushesMaximumDimensions()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();

        Instruction(fp, 'G')(ctx);

        Assert.AreEqual(3, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_A_StoresToSingleDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(42);
        ctx.Push(-2);

        Instruction(fp, 'A')(ctx);

        Assert.AreEqual(42, ctx.GetCell(8, 20, 30));
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void Instruction_B_RetrievesFromSingleDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(12, 20, 30, 99);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(2);

        Instruction(fp, 'B')(ctx);

        Assert.AreEqual(99, ctx.Pop());
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void Instruction_C_StoresToTwoDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(77);
        ctx.Push(-2);
        ctx.Push(3);

        Instruction(fp, 'C')(ctx);

        Assert.AreEqual(77, ctx.GetCell(8, 23, 30));
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void Instruction_D_RetrievesFromTwoDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(8, 23, 30, 55);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(-2);
        ctx.Push(3);

        Instruction(fp, 'D')(ctx);

        Assert.AreEqual(55, ctx.Pop());
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void Instruction_E_StoresToThreeDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(88);
        ctx.Push(1);
        ctx.Push(-2);
        ctx.Push(3);

        Instruction(fp, 'E')(ctx);

        Assert.AreEqual(88, ctx.GetCell(11, 18, 33));
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void Instruction_F_RetrievesFromThreeDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(11, 18, 33, 66);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(1);
        ctx.Push(-2);
        ctx.Push(3);

        Instruction(fp, 'F')(ctx);

        Assert.AreEqual(66, ctx.Pop());
        AssertVector(ctx, 10, 20, 30);
    }

    [TestMethod]
    public void MissingCapabilities_Reflects()
    {
        var fp = new ArrayFingerprint();
        var ctx = new CoreOnlyContext();

        Instruction(fp, 'G')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }
}
