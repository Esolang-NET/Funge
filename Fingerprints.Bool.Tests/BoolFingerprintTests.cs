using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Bool;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class BoolFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(BoolFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new BoolFingerprint().Handprint).IsEqualTo(0x424F4F4C);

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(0, 1, 0)]
    [Arguments(5, 0, 0)]
    [Arguments(2, 3, 1)]
    public async Task Instruction_A_And(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0, 1)]
    [Arguments(1, 0)]
    [Arguments(42, 0)]
    public async Task Instruction_N_Not(int val, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(val);
        await (await Instruction(fp, 'N'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(0, 2, 1)]
    [Arguments(5, 0, 1)]
    [Arguments(2, 3, 1)]
    public async Task Instruction_O_Or(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        await (await Instruction(fp, 'O'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(0, 2, 1)]
    [Arguments(5, 0, 1)]
    [Arguments(2, 3, 0)]
    public async Task Instruction_X_Xor(int a, int b, int expected)
    {
        var fp = new BoolFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        await (await Instruction(fp, 'X'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }
}
