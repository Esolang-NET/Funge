using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Evar;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class EnvironmentVariablesFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(EnvironmentVariablesFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void Push0gnirts(TestContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    static string Pop0gnirts(TestContext ctx)
    {
        var chars = new List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        return new string([.. chars]);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new EnvironmentVariablesFingerprint().Handprint).IsEqualTo(0x45564152);

    [Test]
    public async Task Instruction_P_G_RoundTrip()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        var key = "EVAR_TEST_" + Guid.NewGuid().ToString("N")[..8];
        var value = "hello";
        try
        {
            Push0gnirts(ctx, $"{key}={value}");
            await (await Instruction(fp, 'P'))(ctx);
            await Assert.That(ctx.Reflected).IsFalse();

            Push0gnirts(ctx, key);
            await (await Instruction(fp, 'G'))(ctx);
            await Assert.That(ctx.Reflected).IsFalse();
            await Assert.That(Pop0gnirts(ctx)).IsEqualTo(value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Test]
    public async Task Instruction_G_NotFound_ReturnsEmpty()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "EVAR_SURELY_NOT_SET_XYZ12345");
        await (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Instruction_P_NoEquals_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "NO_EQUALS_HERE");
        await (await Instruction(fp, 'P'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_N_PushesCount()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'N'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsGreaterThan(0);
    }

    [Test]
    public async Task Instruction_V_OutOfRange_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        ctx.Push(int.MaxValue);
        await (await Instruction(fp, 'V'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_V_NegativeIndex_Reflects()
    {
        var fp = new EnvironmentVariablesFingerprint();
        var ctx = new TestContext();
        ctx.Push(-1);
        await (await Instruction(fp, 'V'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
