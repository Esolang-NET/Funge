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
    public void WriteString(string value) => _output.Append(value);
    public string? ReadLine() => _input.Count > 0 ? _input.Dequeue() : null;
    public void Reflect() => Reflected = true;

    public void EnqueueInput(string? value) => _input.Enqueue(value);
    public string Output => _output.ToString();
}

[TestClass]
public class BaseFingerprintTests
{
    static FingerprintInstruction Instruction(BaseFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x42415345, new BaseFingerprint().Handprint);

    [TestMethod]
    [DataRow(5, "101")]
    [DataRow(0, "0")]
    public void Instruction_B_BinaryOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        Instruction(fp, 'B')(ctx);

        Assert.AreEqual(expected, ctx.Output);
        Assert.AreEqual(0, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(255, "FF")]
    [DataRow(16, "10")]
    public void Instruction_H_HexOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        Instruction(fp, 'H')(ctx);

        Assert.AreEqual(expected, ctx.Output);
        Assert.AreEqual(0, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(8, "10")]
    [DataRow(9, "11")]
    public void Instruction_O_OctalOutput(int value, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(value);

        Instruction(fp, 'O')(ctx);

        Assert.AreEqual(expected, ctx.Output);
        Assert.AreEqual(0, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow("101", 2, 5)]
    [DataRow("42", 10, 42)]
    public void Instruction_I_ReadsInputInSpecifiedBase(string input, int baseVal, int expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.EnqueueInput(input);
        ctx.Push(baseVal);

        Instruction(fp, 'I')(ctx);

        Assert.AreEqual(expected, ctx.Pop());
        Assert.AreEqual(string.Empty, ctx.Output);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_I_ReflectsOnEof()
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(2);

        Instruction(fp, 'I')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(5, 2, "101")]
    [DataRow(255, 16, "FF")]
    public void Instruction_N_OutputInBase(int number, int baseVal, string expected)
    {
        var fp = new BaseFingerprint();
        var ctx = new TestContext();
        ctx.Push(number);
        ctx.Push(baseVal);

        Instruction(fp, 'N')(ctx);

        Assert.AreEqual(expected, ctx.Output);
        Assert.AreEqual(0, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }
}
