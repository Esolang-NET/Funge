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

[TestClass]
public class LongIntegerFingerprintTests
{
    static FingerprintInstruction Instruction(LongIntegerFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
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

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x4C4F4E47, new LongIntegerFingerprint().Handprint);

    [TestMethod]
    public void A_AddsLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 3_000_000_000L);
        PushLong(context, 2_000_000_000L);

        Instruction(fingerprint, 'A')(context);

        Assert.AreEqual(5_000_000_000L, PopLong(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void B_ReturnsAbsoluteValue()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, -5_000_000_000L);

        Instruction(fingerprint, 'B')(context);

        Assert.AreEqual(5_000_000_000L, PopLong(context));
    }

    [TestMethod]
    public void D_DividesLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 8_000_000_000L);
        PushLong(context, 2_000_000_000L);

        Instruction(fingerprint, 'D')(context);

        Assert.AreEqual(4L, PopLong(context));
    }

    [TestMethod]
    public void D_ReturnsZeroOnDivisionByZero()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 8_000_000_000L);
        PushLong(context, 0);

        Instruction(fingerprint, 'D')(context);

        Assert.AreEqual(0L, PopLong(context));
    }

    [TestMethod]
    public void E_SignExtendsSingleCell()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        context.Push(-42);

        Instruction(fingerprint, 'E')(context);

        Assert.AreEqual(-42L, PopLong(context));
    }

    [TestMethod]
    [DataRow('L', 1L, 2, 4L)]
    [DataRow('L', 8L, -1, 4L)]
    [DataRow('R', 8L, 1, 4L)]
    [DataRow('R', 1L, -2, 4L)]
    public void ShiftInstructions_HandlePositiveAndNegativeCounts(char instruction, long value, int count, long expected)
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, value);
        context.Push(count);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, PopLong(context));
    }

    [TestMethod]
    public void M_MultipliesLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 3_000_000L);
        PushLong(context, 2_000_000L);

        Instruction(fingerprint, 'M')(context);

        Assert.AreEqual(6_000_000_000_000L, PopLong(context));
    }

    [TestMethod]
    public void N_NegatesLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 5_000_000_000L);

        Instruction(fingerprint, 'N')(context);

        Assert.AreEqual(-5_000_000_000L, PopLong(context));
    }

    [TestMethod]
    public void O_ComputesModulo()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 4L);

        Instruction(fingerprint, 'O')(context);

        Assert.AreEqual(1L, PopLong(context));
    }

    [TestMethod]
    public void O_ReturnsZeroOnDivisionByZero()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 0L);

        Instruction(fingerprint, 'O')(context);

        Assert.AreEqual(0L, PopLong(context));
    }

    [TestMethod]
    public void P_PrintsLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, -5_000_000_000L);

        Instruction(fingerprint, 'P')(context);

        Assert.AreEqual((-5_000_000_000L).ToString(CultureInfo.InvariantCulture) + " ", context.Output);
    }

    [TestMethod]
    public void S_SubtractsLongIntegers()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        PushLong(context, 9L);
        PushLong(context, 4L);

        Instruction(fingerprint, 'S')(context);

        Assert.AreEqual(5L, PopLong(context));
    }

    [TestMethod]
    public void Z_ParsesLongInteger()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        Push0gnirts(context, "-5000000000");

        Instruction(fingerprint, 'Z')(context);

        Assert.AreEqual(-5_000_000_000L, PopLong(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void Z_ReflectsOnInvalidInput()
    {
        var fingerprint = new LongIntegerFingerprint();
        var context = new TestContext();
        Push0gnirts(context, "12x");

        Instruction(fingerprint, 'Z')(context);

        Assert.IsTrue(context.Reflected);
    }
}
