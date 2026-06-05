using System.Text;

namespace Esolang.Funge.Fingerprints.Strn;

sealed class TestContext : IFungeExecutionContext, IFungeInputContext, IFungeOutputContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];
    readonly Queue<string?> _inputLines = new();
    readonly StringBuilder _output = new();

    public bool Reflected { get; set; }
    public (int X, int Y, int Z) StorageOffset { get; set; }
    public int Dimensions => 3;
    public string Output => _output.ToString();

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public void PushString(string value)
    {
        Push(0);
        for (var i = 0; i < value.Length; i++)
            Push(value[i]);
    }

    public string PopString()
    {
        var sb = new StringBuilder();
        int c;
        while ((c = Pop()) != 0)
            sb.Insert(0, (char)c);
        return sb.ToString();
    }

    public void EnqueueInput(string? value) => _inputLines.Enqueue(value);

    public string? ReadLine() => _inputLines.Count > 0 ? _inputLines.Dequeue() : null;

    public void WriteString(string value) => _output.Append(value);

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop();
        var y = Pop();
        var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z)
    {
        Push(x);
        Push(y);
        Push(z);
    }

    public int GetCell(int x, int y, int z) => _cells.TryGetValue((x, y, z), out var value) ? value : ' ';

    public void SetCell(int x, int y, int z, int value) => _cells[(x, y, z)] = value;
}

sealed class CoreOnlyContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

[TestClass]
public class StringFingerprintTests
{
    static FingerprintInstruction Instruction(StringFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x5354524E, new StringFingerprint().Handprint);

    [TestMethod]
    public void A_Append_AppendsBottomStringToUpperString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");
        ctx.PushString("World");

        Instruction(fp, 'A')(ctx);

        Assert.AreEqual("WorldHello", ctx.PopString());
    }

    [TestMethod]
    public void C_Compare_UsesUpperThenBottomString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();

        ctx.PushString("Foo");
        ctx.PushString("foo");
        Instruction(fp, 'C')(ctx);
        Assert.IsGreaterThan(0, ctx.Pop());

        ctx.PushString("bar");
        ctx.PushString("Bar");
        Instruction(fp, 'C')(ctx);
        Assert.IsLessThan(0, ctx.Pop());

        ctx.PushString("qUx");
        ctx.PushString("qUx");
        Instruction(fp, 'C')(ctx);
        Assert.AreEqual(0, ctx.Pop());
    }

    [TestMethod]
    public void D_Display_WritesString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");

        Instruction(fp, 'D')(ctx);

        Assert.AreEqual("Hello", ctx.Output);
    }

    [TestMethod]
    public void F_Search_PushesMatchedSuffixOrEmptyString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Bra");
        ctx.PushString("FooBraBaz");

        Instruction(fp, 'F')(ctx);

        Assert.AreEqual("BraBaz", ctx.PopString());

        ctx.PushString("xyz");
        ctx.PushString("FooBraBaz");
        Instruction(fp, 'F')(ctx);
        Assert.AreEqual(string.Empty, ctx.PopString());
    }

    [TestMethod]
    public void G_Get_ReadsStringUsingStorageOffset()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext { StorageOffset = (10, 20, 30) };
        ctx.SetCell(12, 20, 30, 'H');
        ctx.SetCell(13, 20, 30, 'i');
        ctx.SetCell(14, 20, 30, 0);
        ctx.PushVector(2, 0, 0);

        Instruction(fp, 'G')(ctx);

        Assert.AreEqual("Hi", ctx.PopString());
    }

    [TestMethod]
    public void I_Input_PushesReadLineWithoutTrailingNewline()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.EnqueueInput("Testing, testing.");

        Instruction(fp, 'I')(ctx);

        Assert.AreEqual("Testing, testing.", ctx.PopString());
    }

    [TestMethod]
    public void L_Left_ReturnsLeftmostCharacters()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Baz");
        ctx.Push(2);

        Instruction(fp, 'L')(ctx);

        Assert.AreEqual("Ba", ctx.PopString());
    }

    [TestMethod]
    public void M_Slice_ReturnsRequestedWindow()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("FooBarBaz");
        ctx.Push(3);
        ctx.Push(4);

        Instruction(fp, 'M')(ctx);

        Assert.AreEqual("BarB", ctx.PopString());
    }

    [TestMethod]
    public void N_Length_KeepsStringAndPushesLength()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("foo");

        Instruction(fp, 'N')(ctx);

        Assert.AreEqual(3, ctx.Pop());
        Assert.AreEqual("foo", ctx.PopString());
    }

    [TestMethod]
    public void P_Put_WritesStringAndTerminatorUsingStorageOffset()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext { StorageOffset = (10, 20, 30) };
        ctx.PushString("Hi");
        ctx.PushVector(2, 0, 0);

        Instruction(fp, 'P')(ctx);

        Assert.AreEqual('H', ctx.GetCell(12, 20, 30));
        Assert.AreEqual('i', ctx.GetCell(13, 20, 30));
        Assert.AreEqual(0, ctx.GetCell(14, 20, 30));
    }

    [TestMethod]
    public void R_Right_ReturnsRightmostCharacters()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Baz");
        ctx.Push(2);

        Instruction(fp, 'R')(ctx);

        Assert.AreEqual("az", ctx.PopString());
    }

    [TestMethod]
    public void S_NumberToString_ConvertsToDecimalString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.Push(1234567890);

        Instruction(fp, 'S')(ctx);

        Assert.AreEqual("1234567890", ctx.PopString());
    }

    [TestMethod]
    public void V_StringToNumber_UsesAtoiStyleParsing()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("123abc");

        Instruction(fp, 'V')(ctx);

        Assert.AreEqual(123, ctx.Pop());

        ctx.PushString("abc");
        Instruction(fp, 'V')(ctx);
        Assert.AreEqual(0, ctx.Pop());
    }

    [TestMethod]
    public void MissingCapability_Reflects()
    {
        var fp = new StringFingerprint();
        var ctx = new CoreOnlyContext();
        ctx.Push(0);
        ctx.Push('o');
        ctx.Push('l');
        ctx.Push('l');
        ctx.Push('e');
        ctx.Push('H');

        Instruction(fp, 'D')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }
}
