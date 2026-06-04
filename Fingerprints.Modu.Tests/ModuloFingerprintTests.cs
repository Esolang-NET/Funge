namespace Esolang.Funge.Fingerprints.Modu.Tests;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class ModuloFingerprintTests
{
    static FingerprintInstruction Instruction(ModuloFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x4D4F4455, new ModuloFingerprint().Handprint);

    [TestMethod]
    [DataRow(7, 3, 1)]     // 7 % 3 = 1
    [DataRow(-7, 3, 2)]    // -7 % 3 = 2 (sign of divisor 3)
    [DataRow(7, -3, -2)]   // 7 % -3 = -2 (sign of divisor -3)
    [DataRow(-7, -3, -1)]  // -7 % -3 = -1 (sign of divisor -3)
    [DataRow(7, 0, 0)]     // division by zero
    public void M_SignedResultModulo(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'M')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
    }

    [TestMethod]
    [DataRow(7, 3, 1)]     // 7 % 3 = 1
    [DataRow(-7, 3, -1)]   // -7 % 3 = -1 (sign of dividend -7)
    [DataRow(7, -3, 1)]    // 7 % -3 = 1 (sign of dividend 7)
    [DataRow(-7, -3, -1)]  // -7 % -3 = -1 (sign of dividend -7)
    [DataRow(7, 0, 0)]     // division by zero
    public void R_Remainder(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'R')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
    }

    [TestMethod]
    [DataRow(7, 3, 1)]     // 7 % 3 = 1
    [DataRow(-7, 3, 2)]    // -7 % 3 = 2 (always positive)
    [DataRow(7, -3, 1)]    // 7 % -3 = 1 (always positive)
    [DataRow(-7, -3, 2)]   // -7 % -3 = 2 (always positive)
    [DataRow(7, 0, 0)]     // division by zero
    public void U_UnsignedResultModulo(int a, int b, int expected)
    {
        var fp = new ModuloFingerprint();
        var ctx = new TestContext();
        ctx.Push(a);
        ctx.Push(b);
        Instruction(fp, 'U')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
    }
}
