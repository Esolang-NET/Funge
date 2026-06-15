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

public class RandomFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(RandomFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
        return handler;
    }

    static RandomContext CreateRandomContext(params int[] values)
    {
        var context = new RandomContext();
        foreach (var value in values)
            context.Push(value);

        return context;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new RandomFingerprint().Handprint).IsEqualTo(0x52414E44);

    [Test]
    public async Task I_UsesUnsignedUpperBound()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(-1);

        (await Instruction(fingerprint, 'I'))(context);

        await Assert.That(context.LastUpperBound).IsEqualTo(uint.MaxValue);
        await Assert.That(context.Pop()).IsEqualTo(-2);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task I_ReflectsOnZeroUpperBound()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(0);

        (await Instruction(fingerprint, 'I'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task M_PushesIntMaxValue()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        (await Instruction(fingerprint, 'M'))(context);

        await Assert.That(context.Pop()).IsEqualTo(int.MaxValue);
    }

    [Test]
    public async Task R_PushesSinglePrecisionBits()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(BitConverter.ToSingle(BitConverter.GetBytes(context.Pop()), 0)).IsEqualTo(0.5f);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task S_ReseedsUsingCellBits()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext(-1);

        (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(context.LastSeed).IsEqualTo(uint.MaxValue);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task T_ReseedsFromTime()
    {
        var fingerprint = new RandomFingerprint();
        var context = CreateRandomContext();

        (await Instruction(fingerprint, 'T'))(context);

        await Assert.That(context.TimeReseeded).IsTrue();
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task MissingRandomCapability_Reflects()
    {
        var fingerprint = new RandomFingerprint();
        var context = new CoreOnlyContext();

        (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }
}
