namespace Esolang.Funge.Fingerprints.Roma;

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
public class RomanFingerprintTests
{
    static FingerprintInstruction Instruction(RomanFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x524F4D41, new RomanFingerprint().Handprint);

    [TestMethod]
    [DataRow('I', 1)]
    [DataRow('V', 5)]
    [DataRow('X', 10)]
    [DataRow('L', 50)]
    [DataRow('C', 100)]
    [DataRow('D', 500)]
    [DataRow('M', 1000)]
    public void Instructions_PushCorrectValues(char ch, int expected)
    {
        var fp = new RomanFingerprint();
        var ctx = new TestContext();
        Instruction(fp, ch)(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }
}
