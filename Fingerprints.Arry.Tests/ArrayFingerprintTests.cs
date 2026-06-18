using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Arry;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

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

    public int Dimensions => 3;

    public int GetCell(int x, int y, int z) => _cells.TryGetValue((x, y, z), out var value) ? value : ' ';

    public void SetCell(int x, int y, int z, int value)
    {
        if (value == ' ')
            _cells.Remove((x, y, z));
        else
            _cells[(x, y, z)] = value;
    }
}

sealed class CoreOnlyContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class ArrayFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ArrayFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void PushVector(TestContext ctx, int x, int y, int z) => ctx.PushVector(x, y, z);

    static async Task AssertVector(TestContext ctx, int x, int y, int z)
    {
        await Assert.That(ctx.Pop()).IsEqualTo(z);
        await Assert.That(ctx.Pop()).IsEqualTo(y);
        await Assert.That(ctx.Pop()).IsEqualTo(x);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ArrayFingerprint().Handprint).IsEqualTo(0x41525259);

    [Test]
    public async Task Instruction_G_PushesMaximumDimensions()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();

        await (await Instruction(fp, 'G'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(3);
    }

    [Test]
    public async Task Instruction_A_StoresToSingleDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(42);
        ctx.Push(-2);

        await (await Instruction(fp, 'A'))(ctx);

        await Assert.That(ctx.GetCell(8, 20, 30)).IsEqualTo(42);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task Instruction_B_RetrievesFromSingleDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(12, 20, 30, 99);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(2);

        await (await Instruction(fp, 'B'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(99);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task Instruction_C_StoresToTwoDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(77);
        ctx.Push(-2);
        ctx.Push(3);

        await (await Instruction(fp, 'C'))(ctx);

        await Assert.That(ctx.GetCell(8, 23, 30)).IsEqualTo(77);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task Instruction_D_RetrievesFromTwoDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(8, 23, 30, 55);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(-2);
        ctx.Push(3);

        await (await Instruction(fp, 'D'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(55);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task Instruction_E_StoresToThreeDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        PushVector(ctx, 10, 20, 30);
        ctx.Push(88);
        ctx.Push(1);
        ctx.Push(-2);
        ctx.Push(3);

        await (await Instruction(fp, 'E'))(ctx);

        await Assert.That(ctx.GetCell(11, 18, 33)).IsEqualTo(88);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task Instruction_F_RetrievesFromThreeDimensionArray()
    {
        var fp = new ArrayFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(11, 18, 33, 66);
        PushVector(ctx, 10, 20, 30);
        ctx.Push(1);
        ctx.Push(-2);
        ctx.Push(3);

        await (await Instruction(fp, 'F'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(66);
        await AssertVector(ctx, 10, 20, 30);
    }

    [Test]
    public async Task MissingCapabilities_Reflects()
    {
        var fp = new ArrayFingerprint();
        var ctx = new CoreOnlyContext();

        await (await Instruction(fp, 'G'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }
}
