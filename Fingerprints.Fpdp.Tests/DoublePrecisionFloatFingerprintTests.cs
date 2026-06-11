namespace Esolang.Funge.Fingerprints.Fpdp;

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
public class DoublePrecisionFloatFingerprintTests
{
    static FingerprintInstruction Instruction(DoublePrecisionFloatFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushDouble(TestContext ctx, double value)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        ctx.Push(unchecked((int)(bits & 0xFFFFFFFFL)));
        ctx.Push(unchecked((int)(bits >> 32)));
    }

    static double PopDouble(TestContext ctx)
    {
        var upper = ctx.Pop();
        var lower = ctx.Pop();
        var bits = ((long)upper << 32) | unchecked((uint)lower);
        return BitConverter.Int64BitsToDouble(bits);
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x46504450, new DoublePrecisionFloatFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_Add()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 1.5); PushDouble(ctx, 2.5);
        Instruction(fp, 'A')(ctx);
        Assert.AreEqual(4.0, PopDouble(ctx), 1e-10);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_M_Multiply()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 3.0); PushDouble(ctx, 4.0);
        Instruction(fp, 'M')(ctx);
        Assert.AreEqual(12.0, PopDouble(ctx), 1e-10);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_F_FromInt()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        ctx.Push(100);
        Instruction(fp, 'F')(ctx);
        Assert.AreEqual(100.0, PopDouble(ctx), 1e-10);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Q_Sqrt()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 16.0);
        Instruction(fp, 'Q')(ctx);
        Assert.AreEqual(4.0, PopDouble(ctx), 1e-10);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Y_Pow()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 2.0); // y (base)
        PushDouble(ctx, 10.0); // x (exponent, on top)
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(1024.0, PopDouble(ctx), 1e-6);
        Assert.IsFalse(ctx.Reflected);
    }
}
