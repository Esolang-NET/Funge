using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

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
    public Task<char> ReadCharAsync() => throw new NotImplementedException();
    public Task<int> ReadIntAsync() => throw new NotImplementedException();
    public async Task<string?> ReadLineAsync()
    {
        await Task.Yield();
        return _inputLines.Count > 0 ? _inputLines.Dequeue() : null;
    }
    public async Task WriteStringAsync(string value)
    {
        await Task.Yield();
        _output.Append(value);
    }
    public Task WriteLineAsync(string value) => throw new NotImplementedException();
    public Task WriteCharAsync(char value) => throw new NotImplementedException();
    public Task WriteIntAsync(int value) => throw new NotImplementedException();
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

public class StringFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(StringFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new StringFingerprint().Handprint).IsEqualTo(0x5354524E);

    [Test]
    public async Task A_Append_AppendsBottomStringToUpperString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");
        ctx.PushString("World");

        await (await Instruction(fp, 'A'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("WorldHello");
    }

    [Test]
    public async Task C_Compare_UsesUpperThenBottomString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();

        ctx.PushString("Foo");
        ctx.PushString("foo");
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Pop()).IsGreaterThan(0);

        ctx.PushString("bar");
        ctx.PushString("Bar");
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Pop()).IsLessThan(0);

        ctx.PushString("qUx");
        ctx.PushString("qUx");
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
    }

    [Test]
    public async Task D_Display_WritesString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Hello");

        await (await Instruction(fp, 'D'))(ctx);

        await Assert.That(ctx.Output).IsEqualTo("Hello");
    }

    [Test]
    public async Task F_Search_PushesMatchedSuffixOrEmptyString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Bra");
        ctx.PushString("FooBraBaz");

        await (await Instruction(fp, 'F'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("BraBaz");

        ctx.PushString("xyz");
        ctx.PushString("FooBraBaz");
        await (await Instruction(fp, 'F'))(ctx);
        await Assert.That(ctx.PopString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task G_Get_ReadsStringUsingStorageOffset()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext { StorageOffset = (10, 20, 30) };
        ctx.SetCell(12, 20, 30, 'H');
        ctx.SetCell(13, 20, 30, 'i');
        ctx.SetCell(14, 20, 30, 0);
        ctx.PushVector(2, 0, 0);

        await (await Instruction(fp, 'G'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("Hi");
    }

    [Test]
    public async Task I_Input_PushesReadLineWithoutTrailingNewline()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.EnqueueInput("Testing, testing.");

        await (await Instruction(fp, 'I'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("Testing, testing.");
    }

    [Test]
    public async Task L_Left_ReturnsLeftmostCharacters()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Baz");
        ctx.Push(2);

        await (await Instruction(fp, 'L'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("Ba");
    }

    [Test]
    public async Task M_Slice_ReturnsRequestedWindow()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("FooBarBaz");
        ctx.Push(3);
        ctx.Push(4);

        await (await Instruction(fp, 'M'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("BarB");
    }

    [Test]
    public async Task N_Length_KeepsStringAndPushesLength()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("foo");

        await (await Instruction(fp, 'N'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(3);
        await Assert.That(ctx.PopString()).IsEqualTo("foo");
    }

    [Test]
    public async Task P_Put_WritesStringAndTerminatorUsingStorageOffset()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext { StorageOffset = (10, 20, 30) };
        ctx.PushString("Hi");
        ctx.PushVector(2, 0, 0);

        await (await Instruction(fp, 'P'))(ctx);

        await Assert.That(ctx.GetCell(12, 20, 30)).IsEqualTo('H');
        await Assert.That(ctx.GetCell(13, 20, 30)).IsEqualTo('i');
        await Assert.That(ctx.GetCell(14, 20, 30)).IsEqualTo(0);
    }

    [Test]
    public async Task R_Right_ReturnsRightmostCharacters()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("Baz");
        ctx.Push(2);

        await (await Instruction(fp, 'R'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("az");
    }

    [Test]
    public async Task S_NumberToString_ConvertsToDecimalString()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.Push(1234567890);

        await (await Instruction(fp, 'S'))(ctx);

        await Assert.That(ctx.PopString()).IsEqualTo("1234567890");
    }

    [Test]
    public async Task V_StringToNumber_UsesAtoiStyleParsing()
    {
        var fp = new StringFingerprint();
        var ctx = new TestContext();
        ctx.PushString("123abc");

        await (await Instruction(fp, 'V'))(ctx);

        await Assert.That(ctx.Pop()).IsEqualTo(123);

        ctx.PushString("abc");
        await (await Instruction(fp, 'V'))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(0);
    }

    [Test]
    public async Task MissingCapability_Reflects()
    {
        var fp = new StringFingerprint();
        var ctx = new CoreOnlyContext();
        ctx.Push(0);
        ctx.Push('o');
        ctx.Push('l');
        ctx.Push('l');
        ctx.Push('e');
        ctx.Push('H');

        await (await Instruction(fp, 'D'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }
}
