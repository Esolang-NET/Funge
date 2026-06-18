using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Toys;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext, IFungePositionContext, IFungeStackContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public int StackDepth => _stack.Count;
    public int Dimensions => 2;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) Position { get; set; } = (5, 5, 0);
    public (int X, int Y, int Z) Delta { get; set; } = (1, 0, 0);

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop(); var y = Pop(); var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z) { Push(x); Push(y); Push(z); }
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

public class ToysFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ToysFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ToysFingerprint().Handprint).IsEqualTo(0x544F5953);

    [Test]
    public async Task Instruction_A_Replicate()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(42); // value
        ctx.Push(3);  // n
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(42);
        await Assert.That(ctx.Pop()).IsEqualTo(42);
        await Assert.That(ctx.Pop()).IsEqualTo(42);
    }

    [Test]
    public async Task Instruction_B_Butterfly()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // a
        ctx.Push(5); // b
        await (await Instruction(fp, 'B'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(-2); // a-b
        await Assert.That(ctx.Pop()).IsEqualTo(8);  // a+b
    }

    [Test]
    public async Task Instruction_D_Decrement()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(10);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(9);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_I_Increment()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(9);
        await (await Instruction(fp, 'I'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_N_Negate()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(5);
        await (await Instruction(fp, 'N'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(-5);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_H_Shift()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(4); // a
        ctx.Push(2); // b
        await (await Instruction(fp, 'H'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(16); // 4 << 2
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_E_Sum()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(1); ctx.Push(2); ctx.Push(3);
        await (await Instruction(fp, 'E'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(6);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_P_Product()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(2); ctx.Push(3); ctx.Push(4);
        await (await Instruction(fp, 'P'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(24);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_X_IncrementX()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (3, 4, 0) };
        await (await Instruction(fp, 'X'))(ctx);
        await Assert.That(ctx.Position).IsEqualTo((4, 4, 0));
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Y_IncrementY()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (3, 4, 0) };
        await (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(ctx.Position).IsEqualTo((3, 5, 0));
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_R_PeekRight()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.SetCell(6, 5, 0, 99);
        await (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(99);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_L_PeekLeft()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.SetCell(4, 5, 0, 77);
        await (await Instruction(fp, 'L'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(77);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Q_SetPrevious()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(42);
        await (await Instruction(fp, 'Q'))(ctx);
        await Assert.That(ctx.GetCell(4, 5, 0)).IsEqualTo(42);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_S_FillSpace()
    {
        var fp = new ToysFingerprint();
        var ctx = new TestContext();
        ctx.Push(7); // value
        ctx.Push(3); ctx.Push(2); ctx.Push(0); // size (3,2)
        ctx.Push(0); ctx.Push(0); ctx.Push(0); // dest (0,0,0)
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.GetCell(0, 0, 0)).IsEqualTo(7);
        await Assert.That(ctx.GetCell(2, 1, 0)).IsEqualTo(7);
    }

    [Test]
    public async Task Instruction_E_NoStackContext_Reflects()
    {
        var fp = new ToysFingerprint();
        var ctx = new BasicContext();
        await (await Instruction(fp, 'E'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
