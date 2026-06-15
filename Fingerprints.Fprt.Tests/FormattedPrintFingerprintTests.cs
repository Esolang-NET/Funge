namespace Esolang.Funge.Fingerprints.Fprt;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class FormattedPrintFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(FormattedPrintFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void Push0gnirts(TestContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    static string Pop0gnirts(TestContext ctx)
    {
        var chars = new System.Collections.Generic.List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        return new string([.. chars]);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new FormattedPrintFingerprint().Handprint).IsEqualTo(0x46505254);

    [Test]
    public async Task Instruction_I_FormatInteger()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(42); // value
        Push0gnirts(ctx, "%d"); // format
        (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("42");
    }

    [Test]
    public async Task Instruction_I_HexFormat()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(255); // value
        Push0gnirts(ctx, "%x"); // format
        (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("ff");
    }

    [Test]
    public async Task Instruction_I_NoSpecifier_Reflects()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(42);
        Push0gnirts(ctx, "no specifier");
        (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_S_FormatString()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "world"); // value (bottom)
        Push0gnirts(ctx, "Hello %s!"); // format (top)
        (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("Hello world!");
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


    [Test]
    public async Task Instruction_F_FormatFloat()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(SingleToInt32Bits(3.14f)); // value
        Push0gnirts(ctx, "%.2f"); // format
        (await Instruction(fp, 'F'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var result = Pop0gnirts(ctx);
        await Assert.That(result).StartsWith("3.14", StringComparison.Ordinal);
    }

    [Test]
    public async Task Instruction_I_MultipleSpecifiers_Reflects()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(1);
        Push0gnirts(ctx, "%d %d");
        (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
