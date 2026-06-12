namespace Esolang.Funge.Fingerprints.Evar;

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
public class EnvironmentVariablesFingerprintTests
{
    static FingerprintInstruction Instruction(EnvironmentVariablesFingerprint fp, char ch)
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
        => Assert.AreEqual(0x45564152, new EnvironmentVariablesFingerprint().Handprint);

    [TestMethod]
    public void Instruction_P_G_RoundTrip()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        var key = "EVAR_TEST_" + Guid.NewGuid().ToString("N")[..8];
        var value = "hello";
        try
        {
            Push0gnirts(ctx, $"{key}={value}");
            Instruction(fp, 'P')(ctx);
            Assert.IsFalse(ctx.Reflected);

            Push0gnirts(ctx, key);
            Instruction(fp, 'G')(ctx);
            Assert.IsFalse(ctx.Reflected);
            Assert.AreEqual(value, Pop0gnirts(ctx));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [TestMethod]
    public void Instruction_G_NotFound_ReturnsEmpty()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "EVAR_SURELY_NOT_SET_XYZ12345");
        Instruction(fp, 'G')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(string.Empty, Pop0gnirts(ctx));
    }

    [TestMethod]
    public void Instruction_P_NoEquals_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "NO_EQUALS_HERE");
        Instruction(fp, 'P')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_N_PushesCount()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'N')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.IsGreaterThan(0, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_V_OutOfRange_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        ctx.Push(int.MaxValue);
        Instruction(fp, 'V')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_V_NegativeIndex_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        ctx.Push(-1);
        Instruction(fp, 'V')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
