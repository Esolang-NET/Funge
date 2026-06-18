using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Cpli;

sealed class TestContext : IFungeExecutionContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public StringBuilder Output { get; } = new();
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
    public void WriteString(string s) => Output.Append(s);
}

sealed class NoOutputContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class ComplexIntegerFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ComplexIntegerFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    // Push (a+bi): push real first then imag (imag on top)
    static void PushComplex(TestContext ctx, int real, int imag)
    {
        ctx.Push(real);
        ctx.Push(imag);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ComplexIntegerFingerprint().Handprint).IsEqualTo(0x43504C49);

    [Test]
    public async Task Instruction_A_Add()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (1+2i) + (3+4i) = (4+6i)
        PushComplex(ctx, 1, 2);
        PushComplex(ctx, 3, 4);
        await (await Instruction(fp, 'A'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(6); // imag
        await Assert.That(ctx.Pop()).IsEqualTo(4); // real
    }

    [Test]
    public async Task Instruction_S_Subtract()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (5+7i) - (2+3i) = (3+4i)
        PushComplex(ctx, 5, 7);
        PushComplex(ctx, 2, 3);
        await (await Instruction(fp, 'S'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(4); // imag
        await Assert.That(ctx.Pop()).IsEqualTo(3); // real
    }

    [Test]
    public async Task Instruction_M_Multiply()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (1+2i)*(3+4i) = (3-8) + (4+6)i = -5 + 10i
        PushComplex(ctx, 1, 2);
        PushComplex(ctx, 3, 4);
        await (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(10); // imag
        await Assert.That(ctx.Pop()).IsEqualTo(-5); // real
    }

    [Test]
    public async Task Instruction_D_Divide()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (4+2i)/(1+1i): denom=2, real=(4+2)/2=3, imag=(2-4)/2=-1
        PushComplex(ctx, 4, 2);
        PushComplex(ctx, 1, 1);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(-1); // imag
        await Assert.That(ctx.Pop()).IsEqualTo(3);  // real
    }

    [Test]
    public async Task Instruction_D_DivideByZero_Reflects()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        PushComplex(ctx, 4, 2);
        PushComplex(ctx, 0, 0);
        await (await Instruction(fp, 'D'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_O_Output()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // real
        ctx.Push(4); // imag
        await (await Instruction(fp, 'O'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Output.ToString()).IsEqualTo("3+4i");
    }

    [Test]
    public async Task Instruction_O_NoOutput_Reflects()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new NoOutputContext();
        ctx.Push(1);
        ctx.Push(2);
        await (await Instruction(fp, 'O'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_V_Magnitude()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // real
        ctx.Push(4); // imag
        await (await Instruction(fp, 'V'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(ctx.Pop()).IsEqualTo(5);
    }
}
