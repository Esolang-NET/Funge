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

public class DoublePrecisionFloatFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(DoublePrecisionFloatFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
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

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new DoublePrecisionFloatFingerprint().Handprint).IsEqualTo(0x46504450);

    [Test]
    public async Task Instruction_A_Add()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 1.5); PushDouble(ctx, 2.5);
        (await Instruction(fp, 'A'))(ctx);
        await Assert.That(PopDouble(ctx)).IsEqualTo(4.0).Within(1e-10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_M_Multiply()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 3.0); PushDouble(ctx, 4.0);
        (await Instruction(fp, 'M'))(ctx);
        await Assert.That(PopDouble(ctx)).IsEqualTo(12.0).Within(1e-10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_F_FromInt()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        ctx.Push(100);
        (await Instruction(fp, 'F'))(ctx);
        await Assert.That(PopDouble(ctx)).IsEqualTo(100.0).Within(1e-10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Q_Sqrt()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 16.0);
        (await Instruction(fp, 'Q'))(ctx);
        await Assert.That(PopDouble(ctx)).IsEqualTo(4.0).Within(1e-10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Y_Pow()
    {
        var fp = new DoublePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushDouble(ctx, 2.0); // y (base)
        PushDouble(ctx, 10.0); // x (exponent, on top)
        (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(PopDouble(ctx)).IsEqualTo(1024.0).Within(1e-6);
        await Assert.That(ctx.Reflected).IsFalse();
    }
}
