using System.Globalization;
using System.Text;

namespace Esolang.Funge.Fingerprints.Imth;

sealed class StackContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; private set; }

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;
}

sealed class OutputContext : IFungeExecutionContext, IFungeOutputContext
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
public class IntegerMathFingerprintTests
{
    static FingerprintInstruction Instruction(IntegerMathFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
        return handler;
    }

    static StackContext CreateContext(params int[] values)
    {
        var context = new StackContext();
        foreach (var value in values)
        {
            context.Push(value);
        }

        return context;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x494D5448, new IntegerMathFingerprint().Handprint);

    [TestMethod]
    [DataRow('B', -7, 7)]
    [DataRow('C', 7, 700)]
    [DataRow('E', 3, 30000)]
    [DataRow('H', 2, 2000)]
    [DataRow('T', 9, 90)]
    [DataRow('Z', 7, -7)]
    public void SingleOperandMath_InstructionsProduceExpectedResult(char instruction, int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow(5, 4)]
    [DataRow(-5, -4)]
    [DataRow(0, 0)]
    public void D_DecrementsTowardsZero(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, 'D')(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow(5, 6)]
    [DataRow(-5, -6)]
    [DataRow(0, 0)]
    public void I_IncrementsAwayFromZero(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, 'I')(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow(-9, -1)]
    [DataRow(0, 0)]
    [DataRow(12, 1)]
    public void G_ReturnsSign(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, 'G')(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(5, 120)]
    public void F_ComputesFactorial(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, 'F')(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void F_ReflectsOnNegativeInput()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        Instruction(fingerprint, 'F')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void A_AveragesRequestedValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(2, 4, 6, 3);

        Instruction(fingerprint, 'A')(context);

        Assert.AreEqual(4, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void A_TreatsMissingValuesAsZero()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(2, 4, 4);

        Instruction(fingerprint, 'A')(context);

        Assert.AreEqual(1, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void A_ReturnsZeroForZeroCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        Instruction(fingerprint, 'A')(context);

        Assert.AreEqual(0, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void A_ReflectsOnNegativeCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        Instruction(fingerprint, 'A')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void S_SumsRequestedValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(1, 2, 3, 3);

        Instruction(fingerprint, 'S')(context);

        Assert.AreEqual(6, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void S_ReturnsZeroForZeroCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        Instruction(fingerprint, 'S')(context);

        Assert.AreEqual(0, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void S_ReflectsOnNegativeCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        Instruction(fingerprint, 'S')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    [DataRow('L', 3, 2, 12)]
    [DataRow('L', 8, -1, 4)]
    [DataRow('L', 1, 32, 1)]
    [DataRow('R', 8, 1, 4)]
    [DataRow('R', 3, -2, 12)]
    [DataRow('R', 1, 32, 1)]
    public void ShiftInstructions_RespectDirectionAndModulo(char instruction, int value, int count, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(value, count);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void N_ComputesMinimumAcrossMissingValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(5, -2, 3);

        Instruction(fingerprint, 'N')(context);

        Assert.AreEqual(-2, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void X_ComputesMaximumAcrossMissingValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-5, 2);

        Instruction(fingerprint, 'X')(context);

        Assert.AreEqual(0, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow('N')]
    [DataRow('X')]
    public void MinMax_ReflectsWhenCountIsNotPositive(char instruction)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        Instruction(fingerprint, instruction)(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void U_WritesUnsignedDecimalWithTrailingSpace()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = new OutputContext();
        context.Push(-1);

        Instruction(fingerprint, 'U')(context);

        Assert.AreEqual(uint.MaxValue.ToString(CultureInfo.InvariantCulture) + " ", context.Output);
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void U_ReflectsWithoutOutputCapability()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(5);

        Instruction(fingerprint, 'U')(context);

        Assert.IsTrue(context.Reflected);
    }
}
