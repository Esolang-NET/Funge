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

[TestClass]
public class HighResTimerFingerprintTests
{
    static FingerprintInstruction Instruction(HighResTimerFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x48525449, new HighResTimerFingerprint().Handprint);

    [TestMethod]
    public void Instruction_G_PushesGranularity()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'G')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.IsGreaterThanOrEqualTo(0, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_M_T_MeasuresElapsed()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'M')(ctx);

        Thread.Sleep(5);
        Instruction(fp, 'T')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.IsGreaterThanOrEqualTo(0, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_T_NoMark_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'T')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_E_ErasesMarkSoT_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'M')(ctx);
        Instruction(fp, 'E')(ctx);
        var ctx2 = new TestContext();
        Instruction(fp, 'T')(ctx2);
        Assert.IsTrue(ctx2.Reflected);
    }

    [TestMethod]
    public void Instruction_S_PushesSecondMicroseconds()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext();
        Instruction(fp, 'S')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var val = ctx.Pop();
        Assert.IsGreaterThanOrEqualTo(0, val);
        Assert.IsLessThan(1_000_000, val);
    }

    [TestMethod]
    public void Instruction_M_NoIpContext_Reflects()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new NoIpContext();
        Instruction(fp, 'M')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void OnCloned_CopiesMarkToChild()
    {
        var fp = new HighResTimerFingerprint();
        var parentCtx = new TestContext { InstructionPointerId = 1 };
        Instruction(fp, 'M')(parentCtx);

        fp.OnInstructionPointerCloned(1, 2);

        var childCtx = new TestContext { InstructionPointerId = 2 };

        Thread.Sleep(1);
        Instruction(fp, 'T')(childCtx);
        Assert.IsFalse(childCtx.Reflected);
        Assert.IsGreaterThanOrEqualTo(0, childCtx.Pop());
    }

    [TestMethod]
    public void OnTerminated_RemovesMark()
    {
        var fp = new HighResTimerFingerprint();
        var ctx = new TestContext { InstructionPointerId = 5 };
        Instruction(fp, 'M')(ctx);
        fp.OnInstructionPointerTerminated(5);

        var ctx2 = new TestContext { InstructionPointerId = 5 };
        Instruction(fp, 'T')(ctx2);
        Assert.IsTrue(ctx2.Reflected);
    }
}
