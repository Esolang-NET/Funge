using System.Text;

namespace Esolang.Funge.Fingerprints.Orth;

sealed class TestContext : IFungeExecutionContext, IFungeOutputContext, IFungeSpaceContext, IFungePositionContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }
    public StringBuilder Output { get; } = new();

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
    public void WriteString(string s) => Output.Append(s);

    public int Dimensions => 2;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) Position { get; set; } = (0, 0, 0);
    public (int X, int Y, int Z) Delta { get; set; } = (1, 0, 0);
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

public class OrthogonalFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(OrthogonalFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new OrthogonalFingerprint().Handprint).IsEqualTo(0x4F525448);

    [Test]
    [Arguments(0b1010, 0b1100, 0b1000)]
    public async Task Instruction_A_And(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0b1010, 0b1100, 0b0110)]
    public async Task Instruction_E_Xor(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        (await Instruction(fp, 'E'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0b1010, 0b1100, 0b1110)]
    public async Task Instruction_O_Or(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        (await Instruction(fp, 'O'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_G_GetCell()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(3, 5, 0, 42);
        ctx.Push(5); // y
        ctx.Push(3); // x
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(42);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_P_PutCell()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(7); // y
        ctx.Push(2); // x
        ctx.Push(99); // value
        (await Instruction(fp, 'P'))(ctx);
        await Assert.That(ctx.GetCell(2, 7, 0)).IsEqualTo(99);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_S_WriteString()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(0);
        ctx.Push('H');
        ctx.Push('i');
        ctx.Push('!');
        (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Output.ToString()).IsEqualTo("Hi!");
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_V_SetDeltaX()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(-1);
        (await Instruction(fp, 'V'))(ctx);
        await Assert.That(ctx.Delta.X).IsEqualTo(-1);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_W_SetDeltaY()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(3);
        (await Instruction(fp, 'W'))(ctx);
        await Assert.That(ctx.Delta.Y).IsEqualTo(3);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_X_SetPositionX()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(10);
        (await Instruction(fp, 'X'))(ctx);
        await Assert.That(ctx.Position.X).IsEqualTo(10);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Y_SetPositionY()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(20);
        (await Instruction(fp, 'Y'))(ctx);
        await Assert.That(ctx.Position.Y).IsEqualTo(20);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Z_SkipIfZero()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(0);
        (await Instruction(fp, 'Z'))(ctx);
        await Assert.That(ctx.Position).IsEqualTo((6, 5, 0));
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Z_NonZero_NoSkip()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(1);
        (await Instruction(fp, 'Z'))(ctx);
        await Assert.That(ctx.Position).IsEqualTo((5, 5, 0));
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_G_NoSpace_Reflects()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new BasicContext();
        ctx.Push(0); ctx.Push(0);
        (await Instruction(fp, 'G'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
