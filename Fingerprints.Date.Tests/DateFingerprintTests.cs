namespace Esolang.Funge.Fingerprints.Date;

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
public class DateFingerprintTests
{
    static FingerprintInstruction Instruction(DateFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushDate(TestContext ctx, int year, int month, int day)
    {
        ctx.Push(year);
        ctx.Push(month);
        ctx.Push(day);
    }

    static void AssertDate(TestContext ctx, int year, int month, int day)
    {
        Assert.AreEqual(day, ctx.Pop());
        Assert.AreEqual(month, ctx.Pop());
        Assert.AreEqual(year, ctx.Pop());
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x44415445, new DateFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_AddsDays()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 27);
        ctx.Push(3);

        Instruction(fp, 'A')(ctx);

        AssertDate(ctx, 2024, 3, 1);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_C_ConvertsJulianDayToCalendarDate()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2451545);

        Instruction(fp, 'C')(ctx);

        AssertDate(ctx, 2000, 1, 1);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_D_ComputesDaysBetweenDates()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 28);
        PushDate(ctx, 2024, 3, 1);

        Instruction(fp, 'D')(ctx);

        Assert.AreEqual(2, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_J_ConvertsCalendarDateToJulianDay()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2000, 1, 1);

        Instruction(fp, 'J')(ctx);

        Assert.AreEqual(2451545, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_T_ConvertsZeroBasedDayOfYearToDate()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2024);
        ctx.Push(59);

        Instruction(fp, 'T')(ctx);

        AssertDate(ctx, 2024, 2, 29);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_W_PushesMondayBasedDayOfWeek()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 6, 3);

        Instruction(fp, 'W')(ctx);

        Assert.AreEqual(0, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Y_PushesZeroBasedDayOfYear()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 12, 31);

        Instruction(fp, 'Y')(ctx);

        Assert.AreEqual(365, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void InvalidDate_Reflects()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 30);

        Instruction(fp, 'J')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void InvalidDayOfYear_Reflects()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2023);
        ctx.Push(365);

        Instruction(fp, 'T')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }
}
