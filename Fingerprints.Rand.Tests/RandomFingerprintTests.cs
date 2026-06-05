namespace Esolang.Funge.Fingerprints.Rand;

sealed class RandomContext : IFungeExecutionContext, IFungeRandomContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; private set; }

    public uint? LastUpperBound { get; private set; }

    public uint? LastSeed { get; private set; }

    public bool TimeReseeded { get; private set; }

    public float NextSingleValue { get; set; } = 0.5f;

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;

    public uint NextUInt32(uint exclusiveUpperBound)
    {
        LastUpperBound = exclusiveUpperBound;
        return exclusiveUpperBound == 0 ? 0 : exclusiveUpperBound - 1;
    }

    public float NextSingle() => NextSingleValue;

    public void Reseed(uint seed) => LastSeed = seed;

    public void Reseed() => TimeReseeded = true;
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
public class RandomFingerprintTests
{
    static FingerprintInstruction Instruction(RandomFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
        return handler;
    }

    static RandomContext CreateRandomContext(params int[] values)
    {
        var context = new RandomContext();
        foreach (var value in values)
            context.Push(value);

        return context;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x52414E44, new RandomFingerprint().Handprint);

    [TestMethod]
    public void I_UsesUnsignedUpperBound()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(-1);

        Instruction(fingerprint, 'I')(context);

        Assert.AreEqual(uint.MaxValue, context.LastUpperBound);
        Assert.AreEqual(-2, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void I_ReflectsOnZeroUpperBound()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(0);

        Instruction(fingerprint, 'I')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void M_PushesIntMaxValue()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        Instruction(fingerprint, 'M')(context);

        Assert.AreEqual(int.MaxValue, context.Pop());
    }

    [TestMethod]
    public void R_PushesSinglePrecisionBits()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        Instruction(fingerprint, 'R')(context);

        Assert.AreEqual(0.5f, BitConverter.ToSingle(BitConverter.GetBytes(context.Pop()), 0));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void S_ReseedsUsingCellBits()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(-1);

        Instruction(fingerprint, 'S')(context);

        Assert.AreEqual(uint.MaxValue, context.LastSeed);
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void T_ReseedsFromTime()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        Instruction(fingerprint, 'T')(context);

        Assert.IsTrue(context.TimeReseeded);
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void MissingRandomCapability_Reflects()
    {
        var fingerprint = new RandomFingerprint();
        var context = new CoreOnlyContext();

        Instruction(fingerprint, 'R')(context);

        Assert.IsTrue(context.Reflected);
    }
}
