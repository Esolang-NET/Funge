using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Indv;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];

    public TestContext(int dimensions = 3)
        => Dimensions = dimensions;

    public bool Reflected { get; private set; }

    public int Dimensions { get; }

    public (int X, int Y, int Z) StorageOffset { get; set; }

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

    public bool Reflected { get; private set; }

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;
}

public class IndirectVectorFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(IndirectVectorFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
        return handler;
    }

    static void PushVector(TestContext context, int x, int y, int z)
        => context.PushVector(x, y, z);

    static void SeedStoredVector(TestContext context, int originX, int originY, int originZ, int x, int y, int z)
    {
        switch (context.Dimensions)
        {
            case 1:
                context.SetCell(originX, originY, originZ, x);
                break;
            case 2:
                context.SetCell(originX, originY, originZ, y);
                context.SetCell(originX + 1, originY, originZ, x);
                break;
            default:
                context.SetCell(originX, originY, originZ, z);
                context.SetCell(originX + 1, originY, originZ, y);
                context.SetCell(originX + 2, originY, originZ, x);
                break;
        }
    }

    static async Task AssertVector(TestContext context, int x, int y, int z)
    {
        await Assert.That(context.Pop()).IsEqualTo(z);
        await Assert.That(context.Pop()).IsEqualTo(y);
        await Assert.That(context.Pop()).IsEqualTo(x);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new IndirectVectorFingerprint().Handprint).IsEqualTo(0x494E4456);

    [Test]
    public async Task G_AppliesStorageOffsetToPointerAndTarget()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext
        {
            StorageOffset = (10, 20, 30),
        };
        SeedStoredVector(context, 11, 22, 33, 7, 6, -5);
        context.SetCell(17, 26, 25, 123);
        PushVector(context, 1, 2, 3);

        await (await Instruction(fingerprint, 'G'))(context);

        await Assert.That(context.Pop()).IsEqualTo(123);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_WritesCellThroughIndirectPointer()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext
        {
            StorageOffset = (4, 5, 6),
        };
        SeedStoredVector(context, 5, 5, 6, 7, 8, 9);
        context.Push(55);
        PushVector(context, 1, 0, 0);

        await (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(context.GetCell(11, 13, 15)).IsEqualTo(55);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task V_ReadsVectorUsingLogicalOrder()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext(2);
        SeedStoredVector(context, 3, 4, 0, 9, 4, 0);
        SeedStoredVector(context, 9, 4, 0, 8, -3, 0);
        PushVector(context, 3, 4, 0);

        await (await Instruction(fingerprint, 'V'))(context);

        await AssertVector(context, 8, -3, 0);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task W_WritesVectorUsingLogicalOrder()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext();
        SeedStoredVector(context, 2, 2, 2, 7, 4, -1);
        PushVector(context, 9, 8, 7);
        PushVector(context, 2, 2, 2);

        await (await Instruction(fingerprint, 'W'))(context);

        await Assert.That(context.GetCell(7, 4, -1)).IsEqualTo(7);
        await Assert.That(context.GetCell(8, 4, -1)).IsEqualTo(8);
        await Assert.That(context.GetCell(9, 4, -1)).IsEqualTo(9);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task MissingCapabilities_Reflects()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new CoreOnlyContext();

        await (await Instruction(fingerprint, 'G'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }
}
