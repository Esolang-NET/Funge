using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Base;

sealed class TestContext : IFungeExecutionContext, IFungeInputContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    readonly Queue<string?> _input = new();
    readonly System.Text.StringBuilder _output = new();

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public void EnqueueInput(string? value) => _input.Enqueue(value);
    public Task<char> ReadCharAsync() => throw new NotImplementedException();
    public Task<int> ReadIntAsync() => throw new NotImplementedException();
    public async Task<string?> ReadLineAsync()
    {
        await Task.Yield();
        return _input.Count > 0 ? _input.Dequeue() : null;
    }
    public async Task WriteStringAsync(string value)
    {
        await Task.Yield();
        _output.Append(value);
    }
    public Task WriteLineAsync(string value) => throw new NotImplementedException();
    public Task WriteCharAsync(char value) => throw new NotImplementedException();
    public Task WriteIntAsync(int value) => throw new NotImplementedException();

    public string Output => _output.ToString();
}

public class BaseFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(BaseFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new BaseFingerprint().Handprint).IsEqualTo(0x42415345);

    [Test]
    [Arguments(5, "101")]
    [Arguments(0, "0")]
    public async Task Instruction_B_BinaryOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        await (await Instruction(fp, 'B'))(ctx);

        await Assert.That(ctx.Output).IsEqualTo(expected);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(255, "FF")]
    [Arguments(16, "10")]
    public async Task Instruction_H_HexOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        await (await Instruction(fp, 'H'))(ctx);

        await Assert.That(ctx.Output).IsEqualTo(expected);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(8, "10")]
    [Arguments(9, "11")]
    public async Task Instruction_O_OctalOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        await (await Instruction(fp, 'O'))(ctx);

        await Assert.That(ctx.Output).IsEqualTo(expected);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments("101", 2, 5)]
    [Arguments("42", 10, 42)]
    public async Task Instruction_I_ReadsInputInSpecifiedBase(string input, int baseVal, int expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.EnqueueInput(input);
        ctx.Push(baseVal);

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Output).IsEqualTo(string.Empty);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_I_ReflectsOnEof()
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(2);

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    [Arguments(5, 2, "101")]
    [Arguments(255, 16, "FF")]
    public async Task Instruction_N_OutputInBase(int number, int baseVal, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(number);
        ctx.Push(baseVal);

        await (await Instruction(fp, 'N'))(ctx);

        await Assert.That(ctx.Output).IsEqualTo(expected);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
        await Assert.That(ctx.Reflected).IsFalse();
    }
}
