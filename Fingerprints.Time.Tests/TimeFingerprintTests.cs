namespace Esolang.Funge.Fingerprints.Time;

sealed class TestContext : IFungeExecutionContext, IFungeInstructionPointerContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public int InstructionPointerId { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class TimeFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(TimeFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new TimeFingerprint().Handprint).IsEqualTo(0x54494D45);

    [Test]
    public async Task D_PushesDayOfMonth()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Day;
        (await Instruction(fp, 'D'))(ctx);
        var after = DateTime.Now.Day;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task F_PushesZeroBasedDayOfYear()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.DayOfYear - 1;
        (await Instruction(fp, 'F'))(ctx);
        var after = DateTime.Now.DayOfYear - 1;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task G_And_L_SwitchBetweenGmtAndLocalMode()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext { InstructionPointerId = 1 };

        (await Instruction(fp, 'G'))(ctx);
        var utcYear = DateTime.UtcNow.Year;
        (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(utcYear);

        (await Instruction(fp, 'L'))(ctx);
        var localYear = DateTime.Now.Year;
        (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(localYear);
    }

    [Test]
    public async Task GmtMode_IsScopedPerInstructionPointer_AndCopiedOnClone()
    {
        var fp = new TimeFingerprint();
        var parent = new TestContext { InstructionPointerId = 1 };
        var child = new TestContext { InstructionPointerId = 2 };

        (await Instruction(fp, 'G'))(parent);
        fp.OnInstructionPointerCloned(1, 2);
        fp.OnInstructionPointerTerminated(1);

        var utcYear = DateTime.UtcNow.Year;
        (await Instruction(fp, 'Y'))(child);
        await Assert.That(child.Pop()).IsEqualTo(utcYear);

        (await Instruction(fp, 'L'))(child);
        var localYear = DateTime.Now.Year;
        (await Instruction(fp, 'Y'))(child);
        await Assert.That(child.Pop()).IsEqualTo(localYear);
    }

    [Test]
    public async Task H_PushesHour()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Hour;
        (await Instruction(fp, 'H'))(ctx);
        var after = DateTime.Now.Hour;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task M_PushesMinute()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Minute;
        (await Instruction(fp, 'M'))(ctx);
        var after = DateTime.Now.Minute;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task O_PushesMonth()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Month;
        (await Instruction(fp, 'O'))(ctx);
        var after = DateTime.Now.Month;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task S_PushesSecond()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        var before = DateTime.Now.Second;
        (await Instruction(fp, 'S'))(ctx);
        var after = DateTime.Now.Second;

        await Assert.That(ctx.Pop())
            .IsGreaterThanOrEqualTo(Math.Min(before, after))
            .And.IsLessThanOrEqualTo(Math.Max(before, after));
    }

    [Test]
    public async Task W_PushesSundayBasedDayOfWeek()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();

        (await Instruction(fp, 'W'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo((int)DateTime.Now.DayOfWeek + 1);
    }

    [Test]
    public async Task Y_PushesYear()
    {
        var fp = new TimeFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(DateTime.Now.Year);
    }
}
