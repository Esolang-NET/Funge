namespace Esolang.Funge.Fingerprints.Strn.Tests;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public void PushString(string value)
    {
        Push(0);
        for (var i = 0; i < value.Length; i++)
            Push(value[i]);
    }

    public string PopString()
    {
        var sb = new System.Text.StringBuilder();
        int c;
        while ((c = Pop()) != 0)
            sb.Insert(0, (char)c);
        return sb.ToString();
    }
}

[TestClass]
public class StringFingerprintTests
{
    static FingerprintInstruction Instruction(StringFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x5354524E, new StringFingerprint().Handprint);

    [TestMethod]
    public void A_Append_ConcatenatesStrings()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");
        ctx.PushString("World");
        Instruction(fp, 'A')(ctx);
        Assert.AreEqual("HelloWorld", ctx.PopString());
    }

    [TestMethod]
    public void C_Compare_ReturnsCorrectValues()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();

        // Equal
        ctx.PushString("abc");
        ctx.PushString("abc");
        Instruction(fp, 'C')(ctx);
        Assert.AreEqual(0, ctx.Pop());

        // Less
        ctx.PushString("abc");
        ctx.PushString("abd");
        Instruction(fp, 'C')(ctx);
        Assert.AreEqual(1, ctx.Pop()); // "abd" > "abc"

        // Greater
        ctx.PushString("abd");
        ctx.PushString("abc");
        Instruction(fp, 'C')(ctx);
        Assert.AreEqual(-1, ctx.Pop()); // "abc" < "abd"
    }

    [TestMethod]
    public void L_Length_ReturnsCorrectValue()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");
        Instruction(fp, 'L')(ctx);
        Assert.AreEqual(5, ctx.Pop());
    }

    [TestMethod]
    public void N_NumberToString_ConvertsCorrectly()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.Push(123);
        Instruction(fp, 'N')(ctx);
        Assert.AreEqual("123", ctx.PopString());
    }

    [TestMethod]
    public void R_Reverse_InvertsString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("abc");
        Instruction(fp, 'R')(ctx);
        Assert.AreEqual("cba", ctx.PopString());
    }

    [TestMethod]
    public void S_StringToNumber_ParsesCorrectly()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("123");
        Instruction(fp, 'S')(ctx);
        Assert.AreEqual(123, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void S_StringToNumber_ReflectsOnError()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("abc");
        Instruction(fp, 'S')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
