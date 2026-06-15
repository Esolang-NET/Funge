namespace Esolang.Funge.Fingerprints.Jstr;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public int Dimensions => 2;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop();
        var y = Pop();
        var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z)
    {
        Push(x);
        Push(y);
        Push(z);
    }
}

sealed class BasicContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class JstrFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(JstrFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
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
        => await Assert.That(new JstrFingerprint().Handprint).IsEqualTo(0x4A535452);

    [Test]
    public async Task Instruction_P_G_RoundTrip()
    {
        var fp = new JstrFingerprint();
        var ctx = new TestContext();
        var str = "Hello";

        // Set up for P: push str, then delta (1,0,0), then pos (0,0,0), then n=5
        ctx.Push(0); // 0gnirts terminator
        foreach (var ch in str) ctx.Push(ch); // push chars left-to-right (last pushed = top = last char)
        // delta (1,0,0)
        ctx.Push(1); ctx.Push(0); ctx.Push(0);
        // pos (0,0,0)
        ctx.Push(0); ctx.Push(0); ctx.Push(0);
        ctx.Push(5); // n
        (await Instruction(fp, 'P'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();

        // Now G: push delta (1,0,0), pos (0,0,0), n=5
        ctx.Push(1); ctx.Push(0); ctx.Push(0);
        ctx.Push(0); ctx.Push(0); ctx.Push(0);
        ctx.Push(5);
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Pop0gnirts(ctx)).IsEqualTo("Hello");
    }

    [Test]
    public async Task Instruction_G_NegativeN_Reflects()
    {
        var fp = new JstrFingerprint();
        var ctx = new TestContext();
        ctx.Push(1); ctx.Push(0); ctx.Push(0); // delta
        ctx.Push(0); ctx.Push(0); ctx.Push(0); // pos
        ctx.Push(-1); // n
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_G_NoContexts_Reflects()
    {
        var fp = new JstrFingerprint();
        var ctx = new BasicContext();
        ctx.Push(0);
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
