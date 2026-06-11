using System.Text;

namespace Esolang.Funge.Fingerprints.Orth;

sealed class TestContext : IFungeExecutionContext, IFungeOutputContext, IFungeSpaceContext, IFungePositionContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }
    public StringBuilder Output { get; } = new();

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
    public void WriteString(string s) => Output.Append(s);

    public int Dimensions => 2;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) Position { get; set; } = (0, 0, 0);
    public (int X, int Y, int Z) Delta { get; set; } = (1, 0, 0);
}

sealed class BasicContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class OrthogonalFingerprintTests
{
    static FingerprintInstruction Instruction(OrthogonalFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x4F525448, new OrthogonalFingerprint().Handprint);

    [TestMethod]
    [DataRow(0b1010, 0b1100, 0b1000)]
    public void Instruction_A_And(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        Instruction(fp, 'A')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(0b1010, 0b1100, 0b0110)]
    public void Instruction_E_Xor(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        Instruction(fp, 'E')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    [DataRow(0b1010, 0b1100, 0b1110)]
    public void Instruction_O_Or(int a, int b, int expected)
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(a); ctx.Push(b);
        Instruction(fp, 'O')(ctx);
        Assert.AreEqual(expected, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_G_GetCell()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.SetCell(3, 5, 0, 42);
        ctx.Push(5); // y
        ctx.Push(3); // x
        Instruction(fp, 'G')(ctx);
        Assert.AreEqual(42, ctx.Pop());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_P_PutCell()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(7); // y
        ctx.Push(2); // x
        ctx.Push(99); // value
        Instruction(fp, 'P')(ctx);
        Assert.AreEqual(99, ctx.GetCell(2, 7, 0));
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_S_WriteString()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(0);
        ctx.Push('H');
        ctx.Push('i');
        ctx.Push('!');
        Instruction(fp, 'S')(ctx);
        Assert.AreEqual("Hi!", ctx.Output.ToString());
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_V_SetDeltaX()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(-1);
        Instruction(fp, 'V')(ctx);
        Assert.AreEqual(-1, ctx.Delta.X);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_W_SetDeltaY()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(3);
        Instruction(fp, 'W')(ctx);
        Assert.AreEqual(3, ctx.Delta.Y);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_X_SetPositionX()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(10);
        Instruction(fp, 'X')(ctx);
        Assert.AreEqual(10, ctx.Position.X);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Y_SetPositionY()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext();
        ctx.Push(20);
        Instruction(fp, 'Y')(ctx);
        Assert.AreEqual(20, ctx.Position.Y);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Z_SkipIfZero()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(0);
        Instruction(fp, 'Z')(ctx);
        Assert.AreEqual((6, 5, 0), ctx.Position);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Z_NonZero_NoSkip()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new TestContext { Position = (5, 5, 0), Delta = (1, 0, 0) };
        ctx.Push(1);
        Instruction(fp, 'Z')(ctx);
        Assert.AreEqual((5, 5, 0), ctx.Position);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_G_NoSpace_Reflects()
    {
        var fp = new OrthogonalFingerprint();
        var ctx = new BasicContext();
        ctx.Push(0); ctx.Push(0);
        Instruction(fp, 'G')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
