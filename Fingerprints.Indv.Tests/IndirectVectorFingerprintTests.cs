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

[TestClass]
public class IndirectVectorFingerprintTests
{
    static FingerprintInstruction Instruction(IndirectVectorFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
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

    static void AssertVector(TestContext context, int x, int y, int z)
    {
        Assert.AreEqual(z, context.Pop());
        Assert.AreEqual(y, context.Pop());
        Assert.AreEqual(x, context.Pop());
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x494E4456, new IndirectVectorFingerprint().Handprint);

    [TestMethod]
    public void G_AppliesStorageOffsetToPointerAndTarget()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext
        {
            StorageOffset = (10, 20, 30),
        };
        SeedStoredVector(context, 11, 22, 33, 7, 6, -5);
        context.SetCell(17, 26, 25, 123);
        PushVector(context, 1, 2, 3);

        Instruction(fingerprint, 'G')(context);

        Assert.AreEqual(123, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_WritesCellThroughIndirectPointer()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext
        {
            StorageOffset = (4, 5, 6),
        };
        SeedStoredVector(context, 5, 5, 6, 7, 8, 9);
        context.Push(55);
        PushVector(context, 1, 0, 0);

        Instruction(fingerprint, 'P')(context);

        Assert.AreEqual(55, context.GetCell(11, 13, 15));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void V_ReadsVectorUsingLogicalOrder()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext(2);
        SeedStoredVector(context, 3, 4, 0, 9, 4, 0);
        SeedStoredVector(context, 9, 4, 0, 8, -3, 0);
        PushVector(context, 3, 4, 0);

        Instruction(fingerprint, 'V')(context);

        AssertVector(context, 8, -3, 0);
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void W_WritesVectorUsingLogicalOrder()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new TestContext();
        SeedStoredVector(context, 2, 2, 2, 7, 4, -1);
        PushVector(context, 9, 8, 7);
        PushVector(context, 2, 2, 2);

        Instruction(fingerprint, 'W')(context);

        Assert.AreEqual(7, context.GetCell(7, 4, -1));
        Assert.AreEqual(8, context.GetCell(8, 4, -1));
        Assert.AreEqual(9, context.GetCell(9, 4, -1));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void MissingCapabilities_Reflects()
    {
        var fingerprint = new IndirectVectorFingerprint();
        var context = new CoreOnlyContext();

        Instruction(fingerprint, 'G')(context);

        Assert.IsTrue(context.Reflected);
    }
}
