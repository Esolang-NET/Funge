namespace Esolang.Funge.Fingerprints.Date;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class DateFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(DateFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void PushDate(TestContext ctx, int year, int month, int day)
    {
        ctx.Push(year);
        ctx.Push(month);
        ctx.Push(day);
    }

    static async Task AssertDate(TestContext ctx, int year, int month, int day)
    {
        await Assert.That(ctx.Pop()).IsEqualTo(day);
        await Assert.That(ctx.Pop()).IsEqualTo(month);
        await Assert.That(ctx.Pop()).IsEqualTo(year);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new DateFingerprint().Handprint).IsEqualTo(0x44415445);

    [Test]
    public async Task Instruction_A_AddsDays()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 27);
        ctx.Push(3);

        (await Instruction(fp, 'A'))(ctx);

        await AssertDate(ctx, 2024, 3, 1);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_C_ConvertsJulianDayToCalendarDate()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2451545);

        (await Instruction(fp, 'C'))(ctx);

        await AssertDate(ctx, 2000, 1, 1);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_D_ComputesDaysBetweenDates()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 28);
        PushDate(ctx, 2024, 3, 1);

        (await Instruction(fp, 'D'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(2);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_J_ConvertsCalendarDateToJulianDay()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2000, 1, 1);

        (await Instruction(fp, 'J'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(2451545);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_T_ConvertsZeroBasedDayOfYearToDate()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2024);
        ctx.Push(59);

        (await Instruction(fp, 'T'))(ctx);

        await AssertDate(ctx, 2024, 2, 29);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_W_PushesMondayBasedDayOfWeek()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 6, 3);

        (await Instruction(fp, 'W'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(0);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Y_PushesZeroBasedDayOfYear()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 12, 31);

        (await Instruction(fp, 'Y'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(365);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task InvalidDate_Reflects()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        PushDate(ctx, 2024, 2, 30);

        (await Instruction(fp, 'J'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task InvalidDayOfYear_Reflects()
    {
        var fp = new DateFingerprint();
        var ctx = new TestContext();
        ctx.Push(2023);
        ctx.Push(365);

        (await Instruction(fp, 'T'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }
}
