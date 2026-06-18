using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
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

public class SinglePrecisionFloatFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(SinglePrecisionFloatFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static float Int32BitsToSingle(int bits)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        return BitConverter.Int32BitsToSingle(bits);
#else
        var bytes = BitConverter.GetBytes(bits);
        return BitConverter.ToSingle(bytes, 0);
#endif
    }

    static int SingleToInt32Bits(float value)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        return BitConverter.SingleToInt32Bits(value);
#else
        var bytes = BitConverter.GetBytes(value);
        return BitConverter.ToInt32(bytes, 0);
#endif
    }

    static void PushFloat(TestContext ctx, float value)
        => ctx.Push(SingleToInt32Bits(value));

    static float PopFloat(TestContext ctx)
        => Int32BitsToSingle(ctx.Pop());

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new SinglePrecisionFloatFingerprint().Handprint).EqualTo(0x46505350);

    [Test]
    public async Task Instruction_A_Add()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 1.5f); PushFloat(ctx, 2.5f);
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(4.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_S_Subtract()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 5.0f); PushFloat(ctx, 2.0f);
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(3.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]

    public async Task Instruction_M_MultiplyAsync()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 3.0f); PushFloat(ctx, 4.0f);
        await (await Instruction(fp, 'M'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(12.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]

    public async Task Instruction_D_DivideAsync()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 10.0f); PushFloat(ctx, 4.0f);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(2.5f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_F_FromInt()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        ctx.Push(42);
        await (await Instruction(fp, 'F'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(42.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_I_Truncate()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 3.9f);
        await (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(3);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_N_Negate()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 5.0f);
        await (await Instruction(fp, 'N'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(-5.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Q_Sqrt()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 9.0f);
        await (await Instruction(fp, 'Q'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(3.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_V_Abs()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, -7.5f);
        await (await Instruction(fp, 'V'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(7.5f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Y_Pow()
    {
        var fp = new SinglePrecisionFloatFingerprint();
        var ctx = new TestContext();
        PushFloat(ctx, 2.0f); // y (base)
        PushFloat(ctx, 10.0f); // x (exponent, on top)
        await (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(1024.0f).Within(0.01f);
        await Assert.That(ctx.Reflected).IsFalse();
    }
}
