using System.Globalization;
using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

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

    public async Task WriteStringAsync(string value)
    {
        await Task.Yield();
        _output.Append(value);
    }
    public Task WriteLineAsync(string value) => throw new NotImplementedException();
    public Task WriteCharAsync(char value) => throw new NotImplementedException();
    public Task WriteIntAsync(int value) => throw new NotImplementedException();

    public string Output => _output.ToString();
}

public class IntegerMathFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(IntegerMathFingerprint fingerprint, char instruction)
    {
        await Assert.That(fingerprint.Instructions.TryGetValue(instruction, out var handler)).IsTrue();
        Assert.NotNull(handler);
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

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new IntegerMathFingerprint().Handprint).IsEqualTo(0x494D5448);

    [Test]
    [Arguments('B', -7, 7)]
    [Arguments('C', 7, 700)]
    [Arguments('E', 3, 30000)]
    [Arguments('H', 2, 2000)]
    [Arguments('T', 9, 90)]
    [Arguments('Z', 7, -7)]
    public async Task SingleOperandMath_InstructionsProduceExpectedResult(char instruction, int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments(5, 4)]
    [Arguments(-5, -4)]
    [Arguments(0, 0)]
    public async Task D_DecrementsTowardsZero(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, 'D'))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments(5, 6)]
    [Arguments(-5, -6)]
    [Arguments(0, 0)]
    public async Task I_IncrementsAwayFromZero(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, 'I'))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments(-9, -1)]
    [Arguments(0, 0)]
    [Arguments(12, 1)]
    public async Task G_ReturnsSign(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, 'G'))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments(0, 1)]
    [Arguments(5, 120)]
    public async Task F_ComputesFactorial(int input, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(input);

        await (await Instruction(fingerprint, 'F'))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task F_ReflectsOnNegativeInput()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        await (await Instruction(fingerprint, 'F'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task A_AveragesRequestedValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(2, 4, 6, 3);

        await (await Instruction(fingerprint, 'A'))(context);

        await Assert.That(context.Pop()).IsEqualTo(4);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task A_TreatsMissingValuesAsZero()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(2, 4, 4);

        await (await Instruction(fingerprint, 'A'))(context);

        await Assert.That(context.Pop()).IsEqualTo(1);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task A_ReturnsZeroForZeroCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        await (await Instruction(fingerprint, 'A'))(context);

        await Assert.That(context.Pop()).IsEqualTo(0);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task A_ReflectsOnNegativeCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        await (await Instruction(fingerprint, 'A'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task S_SumsRequestedValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(1, 2, 3, 3);

        await (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(context.Pop()).IsEqualTo(6);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task S_ReturnsZeroForZeroCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        await (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(context.Pop()).IsEqualTo(0);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task S_ReflectsOnNegativeCount()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-1);

        await (await Instruction(fingerprint, 'S'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    [Arguments('L', 3, 2, 12)]
    [Arguments('L', 8, -1, 4)]
    [Arguments('L', 1, 32, 1)]
    [Arguments('R', 8, 1, 4)]
    [Arguments('R', 3, -2, 12)]
    [Arguments('R', 1, 32, 1)]
    public async Task ShiftInstructions_RespectDirectionAndModulo(char instruction, int value, int count, int expected)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(value, count);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Pop()).IsEqualTo(expected);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task N_ComputesMinimumAcrossMissingValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(5, -2, 3);

        await (await Instruction(fingerprint, 'N'))(context);

        await Assert.That(context.Pop()).IsEqualTo(-2);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task X_ComputesMaximumAcrossMissingValues()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(-5, 2);

        await (await Instruction(fingerprint, 'X'))(context);

        await Assert.That(context.Pop()).IsEqualTo(0);
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    [Arguments('N')]
    [Arguments('X')]
    public async Task MinMax_ReflectsWhenCountIsNotPositive(char instruction)
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(0);

        await (await Instruction(fingerprint, instruction))(context);

        await Assert.That(context.Reflected).IsTrue();
    }

    [Test]
    public async Task U_WritesUnsignedDecimalWithTrailingSpace()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = new OutputContext();
        context.Push(-1);

        await (await Instruction(fingerprint, 'U'))(context);

        await Assert.That(context.Output).IsEqualTo(uint.MaxValue.ToString(CultureInfo.InvariantCulture) + " ");
        await Assert.That(context.Reflected).IsFalse();
    }

    [Test]
    public async Task U_ReflectsWithoutOutputCapability()
    {
        var fingerprint = new IntegerMathFingerprint();
        var context = CreateContext(5);

        await (await Instruction(fingerprint, 'U'))(context);

        await Assert.That(context.Reflected).IsTrue();
    }
}
