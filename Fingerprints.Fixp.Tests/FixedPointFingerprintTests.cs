using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
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

public class FixedPointFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(FixedPointFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
        return handler;
    }

    static StackContext CreateContext(params int[] values)
    {
        var context = new StackContext();
        foreach (var value in values)
            context.Push(value);

        return context;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new FixedPointFingerprint().Handprint).IsEqualTo(0x46495850);

    [Test]
    [Arguments('A', 6, 3, 2)]
    [Arguments('O', 6, 3, 7)]
    [Arguments('X', 6, 3, 5)]
    public async Task BitwiseInstructions_ProduceExpectedResult(char instruction, int left, int right, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(left, right);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments('B', 10000, 0)]
    [Arguments('B', 0, 900000)]
    [Arguments('C', 0, 10000)]
    [Arguments('I', 900000, 10000)]
    [Arguments('J', 0, 0)]
    [Arguments('T', 450000, 10000)]
    [Arguments('U', 10000, 450000)]
    public async Task TrigonometricInstructions_UseScaledDegrees(char instruction, int input, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments('B', 10001)]
    [Arguments('J', -10001)]
    public async Task InverseTrigonometricInstructions_ReflectOnOutOfRangeInput(char instruction, int input)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task T_ReflectsAtOddNinetyDegrees()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(900000);

        await (await Instruction(fingerprint, 'T'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    [Arguments('N', 7, -7)]
    [Arguments('V', -7, 7)]
    [Arguments('S', -7, -1)]
    [Arguments('S', 0, 0)]
    [Arguments('S', 7, 1)]
    public async Task UnaryIntegerInstructions_ProduceExpectedResult(char instruction, int input, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task P_MultipliesByPiUsingTruncation()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(10000);

        await (await Instruction(fingerprint, 'P'))(context);

        await Assert.That(context.Pop()).IsEqualTo(31415);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task Q_ComputesIntegerSquareRoot()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(200);

        await (await Instruction(fingerprint, 'Q'))(context);

        await Assert.That(context.Pop()).IsEqualTo(14);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task Q_ReflectsOnNegativeInput()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(-1);

        await (await Instruction(fingerprint, 'Q'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    [Arguments(2, 3, 8)]
    [Arguments(2, -3, 0)]
    [Arguments(-2, 3, -8)]
    [Arguments(-2, -3, 0)]
    [Arguments(-1, -3, -1)]
    [Arguments(-1, -4, 1)]
    [Arguments(2, 0, 1)]
    public async Task R_ComputesIntegerPowers(int basis, int exponent, int expected)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(basis, exponent);

        await (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0, 0)]
    [Arguments(0, -1)]
    public async Task R_ReflectsOnUndefinedZeroPowers(int basis, int exponent)
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(basis, exponent);

        await (await Instruction(fingerprint, 'R'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task D_ReturnsZeroWhenInputIsZero()
    {
        var fingerprint = new FixedPointFingerprint();
        var context = CreateContext(0);

        await (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(context.Pop()).IsEqualTo(0);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task D_ProducesValuesInsidePositiveRange()
    {
        var fingerprint = new FixedPointFingerprint();
        for (var i = 0; i < 32; i++)
        {
            var context = CreateContext(5);
            await (await Instruction(fingerprint, 'D'))(context);
            var value = context.Pop();
            await Assert.That(value is >= 0 and < 5).IsTrue();
        }
    }

    [Test]
    public async Task D_ProducesValuesInsideNegativeRange()
    {
        var fingerprint = new FixedPointFingerprint();
        for (var i = 0; i < 32; i++)
        {
            var context = CreateContext(-5);
            await (await Instruction(fingerprint, 'D'))(context);
            var value = context.Pop();
            await Assert.That(value is >= -5 and < 0).IsTrue();
        }
    }
}
