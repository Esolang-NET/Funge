using System.Globalization;
using System.Text;

namespace Esolang.Funge.Fingerprints.Long;

sealed class TestContext : IFungeExecutionContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    readonly StringBuilder _output = new();

    public bool Reflected { get; private set; }

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;

    public void WriteString(string value) => _output.Append(value);

    public string Output => _output.ToString();
}

public class LongIntegerFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(LongIntegerFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
        return handler;
    }

    static void PushLong(TestContext context, long value)
    {
        context.Push(unchecked((int)(value >> 32)));
        context.Push(unchecked((int)value));
    }

    static long PopLong(TestContext context)
    {
        var low = unchecked((uint)context.Pop());
        var high = context.Pop();
        return ((long)high << 32) | low;
    }

    static void Push0gnirts(TestContext context, string value)
    {
        context.Push(0);
        foreach (var ch in value)
            context.Push(ch);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new LongIntegerFingerprint().Handprint).IsEqualTo(0x4C4F4E47);

    [Test]
    public async Task A_AddsLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 3_000_000_000L);
        PushLong(context, 2_000_000_000L);

        (await Instruction(fingerprint, 'A'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(5_000_000_000L);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task B_ReturnsAbsoluteValue()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, -5_000_000_000L);

        (await Instruction(fingerprint, 'B'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(5_000_000_000L);
    }

    [Test]
    public async Task D_DividesLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 8_000_000_000L);
        PushLong(context, 2_000_000_000L);

        (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(4L);
    }

    [Test]
    public async Task D_ReturnsZeroOnDivisionByZero()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 8_000_000_000L);
        PushLong(context, 0);

        (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(0L);
    }

    [Test]
    public async Task E_SignExtendsSingleCell()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        context.Push(-42);

        (await Instruction(fingerprint, 'E'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(-42L);
    }

    [Test]
    [Arguments('L', 1L, 2, 4L)]
    [Arguments('L', 8L, -1, 4L)]
    [Arguments('R', 8L, 1, 4L)]
    [Arguments('R', 1L, -2, 4L)]
    public async Task ShiftInstructions_HandlePositiveAndNegativeCounts(char instruction, long value, int count, long expected)
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, value);
        context.Push(count);

        (await Instruction(fingerprint, instruction))(context);

        await Assert.That(PopLong(context)).IsEqualTo(expected);
    }

    [Test]
    public async Task M_MultipliesLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 3_000_000L);
        PushLong(context, 2_000_000L);

        (await Instruction(fingerprint, 'M'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(6_000_000_000_000L);
    }

    [Test]
    public async Task N_NegatesLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 5_000_000_000L);

        (await Instruction(fingerprint, 'N'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(-5_000_000_000L);
    }

    [Test]
    public async Task O_ComputesModulo()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 4L);

        (await Instruction(fingerprint, 'O'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(1L);
    }

    [Test]
    public async Task O_ReturnsZeroOnDivisionByZero()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 0L);

        (await Instruction(fingerprint, 'O'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(0L);
    }

    [Test]
    public async Task P_PrintsLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, -5_000_000_000L);

        (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(context.Output).IsEqualTo((-5_000_000_000L).ToString(CultureInfo.InvariantCulture) + " ");
    }

    [Test]
    public async Task S_SubtractsLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 4L);

        (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(5L);
    }

    [Test]
    public async Task Z_ParsesLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        Push0gnirts(context, "-5000000000");

        (await Instruction(fingerprint, 'Z'))(context);

        await Assert.That(PopLong(context)).IsEqualTo(-5_000_000_000L);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task Z_ReflectsOnInvalidInput()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        Push0gnirts(context, "12x");

        (await Instruction(fingerprint, 'Z'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }
}
