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

[TestClass]
public class FormattedPrintFingerprintTests
{
    static FingerprintInstruction Instruction(FormattedPrintFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
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

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x46505254, new FormattedPrintFingerprint().Handprint);

    [TestMethod]
    public void Instruction_I_FormatInteger()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(42); // value
        Push0gnirts(ctx, "%d"); // format
        Instruction(fp, 'I')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual("42", Pop0gnirts(ctx));
    }

    [TestMethod]
    public void Instruction_I_HexFormat()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(255); // value
        Push0gnirts(ctx, "%x"); // format
        Instruction(fp, 'I')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual("ff", Pop0gnirts(ctx));
    }

    [TestMethod]
    public void Instruction_I_NoSpecifier_Reflects()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(42);
        Push0gnirts(ctx, "no specifier");
        Instruction(fp, 'I')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_S_FormatString()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "world"); // value (bottom)
        Push0gnirts(ctx, "Hello %s!"); // format (top)
        Instruction(fp, 'S')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual("Hello world!", Pop0gnirts(ctx));
    }

    [TestMethod]
    public void Instruction_F_FormatFloat()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(BitConverter.SingleToInt32Bits(3.14f)); // value
        Push0gnirts(ctx, "%.2f"); // format
        Instruction(fp, 'F')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = Pop0gnirts(ctx);
        Assert.IsTrue(result.StartsWith("3.14", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void Instruction_I_MultipleSpecifiers_Reflects()
    {
        var fp = new FormattedPrintFingerprint();
        var ctx = new TestContext();
        ctx.Push(1);
        Push0gnirts(ctx, "%d %d");
        Instruction(fp, 'I')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
