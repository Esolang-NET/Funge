using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

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
    public async Task WriteStringAsync(string value)
    {
        await Task.Yield();
        Output.Append(value);
    }
    public Task WriteLineAsync(string value) => throw new NotImplementedException();
    public Task WriteCharAsync(char value) => throw new NotImplementedException();
    public Task WriteIntAsync(int value) => throw new NotImplementedException();
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

public class TerminalFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(TerminalFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new TerminalFingerprint().Handprint).IsEqualTo(0x5445524D);

    [Test]
    public async Task Instruction_C_ClearsScreen()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("\x1b[2J");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_H_Home()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'H'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("\x1b[H");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_G_GotoPosition()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(2); // r
        ctx.Push(4); // c
        await (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("\x1b[3;5H");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(3, "\x1b[3B")]
    [Arguments(-2, "\x1b[2A")]
    public async Task Instruction_D_CursorDown(int n, string expected)
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(n);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_D_Zero_NoOutput()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(0);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(3, "\x1b[3A")]
    [Arguments(-2, "\x1b[2B")]
    public async Task Instruction_U_CursorUp(int n, string expected)
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        ctx.Push(n);
        await (await Instruction(fp, 'U'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_L_ClearToEndOfLine()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'L'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("\x1b[K");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_S_ClearToEndOfScreen()
    {
        var fp = new TerminalFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("\x1b[J");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_C_NoOutput_Reflects()
    {
        var fp = new TerminalFingerprint();
        var ctx = new NoOutputContext();
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
