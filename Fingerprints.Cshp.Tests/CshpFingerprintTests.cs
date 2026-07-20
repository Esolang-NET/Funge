using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Cshp;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class CshpFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(CshpFingerprint fp, char ch)
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
        => await Assert.That(new CshpFingerprint().Handprint).IsEqualTo(0x43534850);

    [Test]
    public async Task Instruction_S_ReturnsAvailable()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(0);
    }

    [Test]
    public async Task Instruction_E_EvaluatesToString()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "1+2");

        await (await Instruction(fp, 'E'))(ctx);

        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("3");
    }

    [Test]
    public async Task Instruction_E_EvaluatesMultiStatementScript()
    {
        Console.WriteLine();
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "var x = 1; var y = 2; x + y");

        await (await Instruction(fp, 'E'))(ctx);

        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("3");
    }

    [Test]
    public async Task Instruction_E_InvalidCode_Reflects()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "invalid +");

        await (await Instruction(fp, 'E'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_I_EvaluatesToInt()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "40 + 2");

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(42);
    }

    [Test]
    public async Task Instruction_I_EvaluatesExplicitReturn()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "var x = 21; return x * 2;");

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(42);
    }

    [Test]
    public async Task Instruction_I_NonNumeric_Reflects()
    {
        var fp = new CshpFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "\"hello\"");

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }
}
