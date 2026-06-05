namespace Esolang.Funge.Fingerprints.Time;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void WriteString(string value) => throw new NotImplementedException();
    public string? ReadLine() => throw new NotImplementedException();
    public void Reflect() => Reflected = true;
}

[TestClass]
public class TimeFingerprintTests
{
    static FingerprintInstruction Instruction(TimeFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x54494D45, new TimeFingerprint().Handprint);

    [TestMethod]
    public void D_Date_PushesCorrectOrder()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'D')(ctx);
        var y = ctx.Pop();
        var m = ctx.Pop();
        var d = ctx.Pop();
        var now = DateTime.Now;
        Assert.AreEqual(now.Year, y);
        Assert.AreEqual(now.Month, m);
        Assert.AreEqual(now.Day, d);
    }

    [TestMethod]
    public void T_Time_PushesCorrectOrder()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'T')(ctx);
        var h = ctx.Pop();
        var m = ctx.Pop();
        var s = ctx.Pop();
        var now = DateTime.Now;
        Assert.AreEqual(now.Hour, h);
        Assert.AreEqual(now.Minute, m);
        // Second might change between now and execution, so we allow +/- 1
        Assert.IsLessThanOrEqualTo(1, Math.Abs(now.Second - s));
    }

    [TestMethod]
    public void Y_Year_IsReasonable()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(DateTime.Now.Year, ctx.Pop());
    }
}
