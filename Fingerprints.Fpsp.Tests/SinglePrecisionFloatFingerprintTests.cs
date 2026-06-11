namespace Esolang.Funge.Fingerprints.Fpsp;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class SinglePrecisionFloatFingerprintTests
{
    static FingerprintInstruction Instruction(SinglePrecisionFloatFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushFloat(TestContext ctx, float value)
        => ctx.Push(BitConverter.SingleToInt32Bits(value));

    static float PopFloat(TestContext ctx)
        => BitConverter.Int32BitsToSingle(ctx.Pop());

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x46505350, new SinglePrecisionFloatFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_Add()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 1.5f); PushFloat(ctx, 2.5f);
        Instruction(fp, 'A')(ctx);
        Assert.AreEqual(4.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_S_Subtract()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 5.0f); PushFloat(ctx, 2.0f);
        Instruction(fp, 'S')(ctx);
        Assert.AreEqual(3.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_M_Multiply()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 3.0f); PushFloat(ctx, 4.0f);
        Instruction(fp, 'M')(ctx);
        Assert.AreEqual(12.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_D_Divide()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 10.0f); PushFloat(ctx, 4.0f);
        Instruction(fp, 'D')(ctx);
        Assert.AreEqual(2.5f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_F_FromInt()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        ctx.Push(42);
        Instruction(fp, 'F')(ctx);
        Assert.AreEqual(42.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_I_Truncate()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 3.9f);
        Instruction(fp, 'I')(ctx);
        Assert.AreEqual(3, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_N_Negate()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 5.0f);
        Instruction(fp, 'N')(ctx);
        Assert.AreEqual(-5.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Q_Sqrt()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 9.0f);
        Instruction(fp, 'Q')(ctx);
        Assert.AreEqual(3.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_V_Abs()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, -7.5f);
        Instruction(fp, 'V')(ctx);
        Assert.AreEqual(7.5f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Y_Pow()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 2.0f); // y (base)
        PushFloat(ctx, 10.0f); // x (exponent, on top)
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(1024.0f, PopFloat(ctx), 0.01f);
        Assert.IsFalse(ctx.Reflected);
    }
}
