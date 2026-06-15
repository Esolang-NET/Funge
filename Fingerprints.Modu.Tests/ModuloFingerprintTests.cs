namespace Esolang.Funge.Fingerprints.Modu;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class ModuloFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ModuloFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ModuloFingerprint().Handprint).IsEqualTo(0x4D4F4455);

    [Test]
    [Arguments(7, 3, 1)]     // 7 % 3 = 1
    [Arguments(-7, 3, 2)]    // -7 % 3 = 2 (sign of divisor 3)
    [Arguments(7, -3, -2)]   // 7 % -3 = -2 (sign of divisor -3)
    [Arguments(-7, -3, -1)]  // -7 % -3 = -1 (sign of divisor -3)
    [Arguments(7, 0, 0)]     // division by zero
    public async Task M_SignedResultModulo(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(7, 3, 1)]     // 7 % 3 = 1
    [Arguments(-7, 3, -1)]   // -7 % 3 = -1 (sign of dividend -7)
    [Arguments(7, -3, 1)]    // 7 % -3 = 1 (sign of dividend 7)
    [Arguments(-7, -3, -1)]  // -7 % -3 = -1 (sign of dividend -7)
    [Arguments(7, 0, 0)]     // division by zero
    public async Task R_Remainder(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(7, 3, 1)]     // 7 % 3 = 1
    [Arguments(-7, 3, 2)]    // -7 % 3 = 2 (always positive)
    [Arguments(7, -3, 1)]    // 7 % -3 = 1 (always positive)
    [Arguments(-7, -3, 2)]   // -7 % -3 = 2 (always positive)
    [Arguments(7, 0, 0)]     // division by zero
    public async Task U_UnsignedResultModulo(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        (await Instruction(fp, 'U'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
    }
}
