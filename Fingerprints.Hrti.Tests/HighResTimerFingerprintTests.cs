namespace Esolang.Funge.Fingerprints.Hrti;

sealed class TestContext : IFungeExecutionContext, IFungeInstructionPointerContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public int InstructionPointerId { get; set; } = 1;
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

sealed class NoIpContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class HighResTimerFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(HighResTimerFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new HighResTimerFingerprint().Handprint).IsEqualTo(0x48525449);

    [Test]
    public async Task Instruction_G_PushesGranularity()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Instruction_M_T_MeasuresElapsed()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'M'))(ctx);

        Thread.Sleep(5);
        (await Instruction(fp, 'T'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Instruction_T_NoMark_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'T'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_E_ErasesMarkSoT_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'M'))(ctx);
        (await Instruction(fp, 'E'))(ctx);
        var ctx2 = new TestContext();
        (await Instruction(fp, 'T'))(ctx2);
        await Assert.That(ctx2.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_S_PushesSecondMicroseconds()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        var val = ctx.Pop();
        await Assert.That(val).IsGreaterThanOrEqualTo(0);
        await Assert.That(val).IsLessThan(1_000_000);
    }

    [Test]
    public async Task Instruction_M_NoIpContext_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new NoIpContext();
        (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task OnCloned_CopiesMarkToChild()
    {
        var fp = new HighResTimerFingerprint();
        var parentCtx = new TestContext { InstructionPointerId = 1 };
        (await Instruction(fp, 'M'))(parentCtx);

        fp.OnInstructionPointerCloned(1, 2);

        var childCtx = new TestContext { InstructionPointerId = 2 };

        Thread.Sleep(1);
        (await Instruction(fp, 'T'))(childCtx);
        await Assert.That(childCtx.Reflected).IsFalse();
        await Assert.That(childCtx.Pop()).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task OnTerminated_RemovesMark()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext { InstructionPointerId = 5 };
        (await Instruction(fp, 'M'))(ctx);
        fp.OnInstructionPointerTerminated(5);

        var ctx2 = new TestContext { InstructionPointerId = 5 };
        (await Instruction(fp, 'T'))(ctx2);
        await Assert.That(ctx2.Reflected).IsTrue();
    }
}
