namespace Esolang.Funge.Fingerprints.Time;

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
public class TimeFingerprintTests
{
    static FingerprintInstruction Instruction(TimeFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static bool IsWithin(int value, int lower, int upper)
        => lower <= upper
            ? value >= lower && value <= upper
            : value >= lower || value <= upper;

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x54494D45, new TimeFingerprint().Handprint);

    [TestMethod]
    public void D_PushesDayOfMonth()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Day;
        Instruction(fp, 'D')(ctx);
        var after = DateTime.Now.Day;

        Assert.IsTrue(IsWithin(ctx.Pop(), Math.Min(before, after), Math.Max(before, after)));
    }

    [TestMethod]
    public void F_PushesZeroBasedDayOfYear()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.DayOfYear - 1;
        Instruction(fp, 'F')(ctx);
        var after = DateTime.Now.DayOfYear - 1;

        Assert.IsTrue(IsWithin(ctx.Pop(), Math.Min(before, after), Math.Max(before, after)));
    }

    [TestMethod]
    public void G_And_L_SwitchBetweenGmtAndLocalMode()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();

        Instruction(fp, 'G')(ctx);
        var utcYear = DateTime.UtcNow.Year;
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(utcYear, ctx.Pop());

        Instruction(fp, 'L')(ctx);
        var localYear = DateTime.Now.Year;
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(localYear, ctx.Pop());
    }

    [TestMethod]
    public void H_PushesHour()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Hour;
        Instruction(fp, 'H')(ctx);
        var after = DateTime.Now.Hour;

        Assert.IsTrue(IsWithin(ctx.Pop(), Math.Min(before, after), Math.Max(before, after)));
    }

    [TestMethod]
    public void M_PushesMinute()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Minute;
        Instruction(fp, 'M')(ctx);
        var after = DateTime.Now.Minute;

        Assert.IsTrue(IsWithin(ctx.Pop(), Math.Min(before, after), Math.Max(before, after)));
    }

    [TestMethod]
    public void O_PushesMonth()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Month;
        Instruction(fp, 'O')(ctx);
        var after = DateTime.Now.Month;

        Assert.IsTrue(IsWithin(ctx.Pop(), Math.Min(before, after), Math.Max(before, after)));
    }

    [TestMethod]
    public void S_PushesSecond()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Second;
        Instruction(fp, 'S')(ctx);
        var after = DateTime.Now.Second;

        Assert.IsTrue(IsWithin(ctx.Pop(), before, after));
    }

    [TestMethod]
    public void W_PushesSundayBasedDayOfWeek()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();

        Instruction(fp, 'W')(ctx);

        Assert.AreEqual((int)DateTime.Now.DayOfWeek + 1, ctx.Pop());
    }

    [TestMethod]
    public void Y_PushesYear()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(DateTime.Now.Year, ctx.Pop());
    }
}
