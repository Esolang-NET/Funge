namespace Esolang.Funge.Fingerprints.Fixp;

sealed class StackContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; private set; }

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;
}

[TestClass]
public class FixedPointFingerprintTests
{
    static FingerprintInstruction Instruction(FixedPointFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
        return handler;
    }

    static StackContext CreateContext(params int[] values)
    {
        var context = new StackContext();
        foreach (var value in values)
            context.Push(value);

        return context;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x46495850, new FixedPointFingerprint().Handprint);

    [TestMethod]
    [DataRow('A', 6, 3, 2)]
    [DataRow('O', 6, 3, 7)]
    [DataRow('X', 6, 3, 5)]
    public void BitwiseInstructions_ProduceExpectedResult(char instruction, int left, int right, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(left, right);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow('B', 10000, 0)]
    [DataRow('B', 0, 900000)]
    [DataRow('C', 0, 10000)]
    [DataRow('I', 900000, 10000)]
    [DataRow('J', 0, 0)]
    [DataRow('T', 450000, 10000)]
    [DataRow('U', 10000, 450000)]
    public void TrigonometricInstructions_UseScaledDegrees(char instruction, int input, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow('B', 10001)]
    [DataRow('J', -10001)]
    public void InverseTrigonometricInstructions_ReflectOnOutOfRangeInput(char instruction, int input)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, instruction)(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void T_ReflectsAtOddNinetyDegrees()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(900000);

        Instruction(fingerprint, 'T')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    [DataRow('N', 7, -7)]
    [DataRow('V', -7, 7)]
    [DataRow('S', -7, -1)]
    [DataRow('S', 0, 0)]
    [DataRow('S', 7, 1)]
    public void UnaryIntegerInstructions_ProduceExpectedResult(char instruction, int input, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        Instruction(fingerprint, instruction)(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_MultipliesByPiUsingTruncation()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(10000);

        Instruction(fingerprint, 'P')(context);

        Assert.AreEqual(31415, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void Q_ComputesIntegerSquareRoot()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(200);

        Instruction(fingerprint, 'Q')(context);

        Assert.AreEqual(14, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void Q_ReflectsOnNegativeInput()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(-1);

        Instruction(fingerprint, 'Q')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    [DataRow(2, 3, 8)]
    [DataRow(2, -3, 0)]
    [DataRow(-2, 3, -8)]
    [DataRow(-2, -3, 0)]
    [DataRow(-1, -3, -1)]
    [DataRow(-1, -4, 1)]
    [DataRow(2, 0, 1)]
    public void R_ComputesIntegerPowers(int basis, int exponent, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(basis, exponent);

        Instruction(fingerprint, 'R')(context);

        Assert.AreEqual(expected, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, -1)]
    public void R_ReflectsOnUndefinedZeroPowers(int basis, int exponent)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(basis, exponent);

        Instruction(fingerprint, 'R')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void D_ReturnsZeroWhenInputIsZero()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(0);

        Instruction(fingerprint, 'D')(context);

        Assert.AreEqual(0, context.Pop());
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void D_ProducesValuesInsidePositiveRange()
    {
        var fingerprint = new FixedPointFingerprint();
        for (var i = 0; i < 32; i++)
        {
            var context = CreateContext(5);
            Instruction(fingerprint, 'D')(context);
            var value = context.Pop();
            Assert.IsTrue(value is >= 0 and < 5);
        }
    }

    [TestMethod]
    public void D_ProducesValuesInsideNegativeRange()
    {
        var fingerprint = new FixedPointFingerprint();
        for (var i = 0; i < 32; i++)
        {
            var context = CreateContext(-5);
            Instruction(fingerprint, 'D')(context);
            var value = context.Pop();
            Assert.IsTrue(value is >= -5 and < 0);
        }
    }
}
