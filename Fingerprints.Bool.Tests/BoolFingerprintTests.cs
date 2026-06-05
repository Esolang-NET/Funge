namespace Esolang.Funge.Fingerprints.Bool;

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
public class BoolFingerprintTests
{
    static FingerprintInstruction Instruction(BoolFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x424F4F4C, new BoolFingerprint().Handprint);

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(0, 1, 0)]
    [DataRow(5, 0, 0)]
    [DataRow(2, 3, 1)]
    public void Instruction_A_And(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'A')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 0)]
    [DataRow(42, 0)]
    public void Instruction_N_Not(int val, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(val);
        Instruction(fp, 'N')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(0, 2, 1)]
    [DataRow(5, 0, 1)]
    [DataRow(2, 3, 1)]
    public void Instruction_O_Or(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'O')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(0, 2, 1)]
    [DataRow(5, 0, 1)]
    [DataRow(2, 3, 0)]
    public void Instruction_X_Xor(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'X')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }
}
