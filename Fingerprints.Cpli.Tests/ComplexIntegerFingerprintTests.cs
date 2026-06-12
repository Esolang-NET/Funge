using System.Text;

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

[TestClass]
public class ComplexIntegerFingerprintTests
{
    static FingerprintInstruction Instruction(ComplexIntegerFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    // Push (a+bi): push real first then imag (imag on top)
    static void PushComplex(TestContext ctx, int real, int imag)
    {
        ctx.Push(real);
        ctx.Push(imag);
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x43504C49, new ComplexIntegerFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_Add()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (1+2i) + (3+4i) = (4+6i)
        PushComplex(ctx, 1, 2);
        PushComplex(ctx, 3, 4);
        Instruction(fp, 'A')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(6, ctx.Pop()); // imag
        Assert.AreEqual(4, ctx.Pop()); // real
    }

    [TestMethod]
    public void Instruction_S_Subtract()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (5+7i) - (2+3i) = (3+4i)
        PushComplex(ctx, 5, 7);
        PushComplex(ctx, 2, 3);
        Instruction(fp, 'S')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(4, ctx.Pop()); // imag
        Assert.AreEqual(3, ctx.Pop()); // real
    }

    [TestMethod]
    public void Instruction_M_Multiply()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (1+2i)*(3+4i) = (3-8) + (4+6)i = -5 + 10i
        PushComplex(ctx, 1, 2);
        PushComplex(ctx, 3, 4);
        Instruction(fp, 'M')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(10, ctx.Pop()); // imag
        Assert.AreEqual(-5, ctx.Pop()); // real
    }

    [TestMethod]
    public void Instruction_D_Divide()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        // (4+2i)/(1+1i): denom=2, real=(4+2)/2=3, imag=(2-4)/2=-1
        PushComplex(ctx, 4, 2);
        PushComplex(ctx, 1, 1);
        Instruction(fp, 'D')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(-1, ctx.Pop()); // imag
        Assert.AreEqual(3, ctx.Pop());  // real
    }

    [TestMethod]
    public void Instruction_D_DivideByZero_Reflects()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        PushComplex(ctx, 4, 2);
        PushComplex(ctx, 0, 0);
        Instruction(fp, 'D')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_O_Output()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // real
        ctx.Push(4); // imag
        Instruction(fp, 'O')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual("3+4i", ctx.Output.ToString());
    }

    [TestMethod]
    public void Instruction_O_NoOutput_Reflects()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new NoOutputContext();
        ctx.Push(1);
        ctx.Push(2);
        Instruction(fp, 'O')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_V_Magnitude()
    {
        var fp = new ComplexIntegerFingerprint();
        var ctx = new TestContext();
        ctx.Push(3); // real
        ctx.Push(4); // imag
        Instruction(fp, 'V')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(5, ctx.Pop());
    }
}
