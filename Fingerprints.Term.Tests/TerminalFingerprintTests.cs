using System.Text;

namespace Esolang.Funge.Fingerprints.Term;

sealed class TestContext : IFungeExecutionContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public StringBuilder Output { get; } = new();
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
    public void WriteString(string s) => Output.Append(s);
}

sealed class NoOutputContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class TerminalFingerprintTests
{
    static FingerprintInstruction Instruction(TerminalFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x5445524D, new TerminalFingerprint().Handprint);

    [TestMethod]
    public void Instruction_C_ClearsScreen()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'C')(ctx);
        Assert.AreEqual("\x1b[2J", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_H_Home()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'H')(ctx);
        Assert.AreEqual("\x1b[H", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_G_GotoPosition()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(2); // r
        ctx.Push(4); // c
        Instruction(fp, 'G')(ctx);
        Assert.AreEqual("\x1b[3;5H", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(3, "\x1b[3B")]
    [DataRow(-2, "\x1b[2A")]
    public void Instruction_D_CursorDown(int n, string expected)
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(n);
        Instruction(fp, 'D')(ctx);
        Assert.AreEqual(expected, ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_D_Zero_NoOutput()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(0);
        Instruction(fp, 'D')(ctx);
        Assert.AreEqual("", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(3, "\x1b[3A")]
    [DataRow(-2, "\x1b[2B")]
    public void Instruction_U_CursorUp(int n, string expected)
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(n);
        Instruction(fp, 'U')(ctx);
        Assert.AreEqual(expected, ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_L_ClearToEndOfLine()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'L')(ctx);
        Assert.AreEqual("\x1b[K", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_S_ClearToEndOfScreen()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'S')(ctx);
        Assert.AreEqual("\x1b[J", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_C_NoOutput_Reflects()
    {
        var fp = new TerminalFingerprint();
        var ctx = new NoOutputContext();
        Instruction(fp, 'C')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
