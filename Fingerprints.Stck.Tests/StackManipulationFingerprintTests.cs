using System.Text;

namespace Esolang.Funge.Fingerprints.Stck;

sealed class TestContext : IFungeExecutionContext, IFungeStackContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext, IFungeOutputContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _space = [];
    readonly StringBuilder _output = new();

    public bool Reflected { get; private set; }

    public int StackDepth => _stack.Count;

    public (int X, int Y, int Z) StorageOffset { get; set; }

    public int Dimensions => 3;

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;

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

    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var value) ? value : 32;

    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public void WriteString(string value) => _output.Append(value);

    public string Output => _output.ToString();
}

[TestClass]
public class StackManipulationFingerprintTests
{
    static FingerprintInstruction Instruction(StackManipulationFingerprint fingerprint, char instruction)
    {
        Assert.IsTrue(fingerprint.Instructions.TryGetValue(instruction, out var handler));
        return handler;
    }

    static TestContext CreateContext(params int[] values)
    {
        var context = new TestContext();
        foreach (var value in values)
            context.Push(value);
        return context;
    }

    static int[] PopAll(TestContext context)
    {
        List<int> values = [];
        while (context.StackDepth > 0)
            values.Add(context.Pop());

        values.Reverse();
        return [.. values];
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x5354434B, new StackManipulationFingerprint().Handprint);

    [TestMethod]
    public void B_BuriesValueAtRequestedDepth()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 99, 2);

        Instruction(fingerprint, 'B')(context);

        CollectionAssert.AreEqual(new[] { 1, 99, 2, 3 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void B_WithNegativeDepth_PushesZeroesAboveValue()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(7, 42, -2);

        Instruction(fingerprint, 'B')(context);

        CollectionAssert.AreEqual(new[] { 7, 42, 0, 0 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void B_ReflectsWhenRequestedDepthIsTooDeep()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 99, 3);

        Instruction(fingerprint, 'B')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void C_PushesCurrentStackDepth()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        Instruction(fingerprint, 'C')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 3 }, PopAll(context));
    }

    [TestMethod]
    public void D_DuplicatesTopNValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 3);

        Instruction(fingerprint, 'D')(context);

        CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void G_ReproducesValuesWrittenByW()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(10, 20, 30);
        context.StorageOffset = (10, 0, 0);
        context.Push(3);
        context.PushVector(1, 0, 0);
        context.PushVector(1, 0, 0);

        Instruction(fingerprint, 'W')(context);

        Assert.AreEqual(30, context.GetCell(11, 0, 0));
        Assert.AreEqual(20, context.GetCell(12, 0, 0));
        Assert.AreEqual(10, context.GetCell(13, 0, 0));

        context.Push(3);
        context.PushVector(1, 0, 0);
        context.PushVector(1, 0, 0);

        Instruction(fingerprint, 'G')(context);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void K_MovesRequestedBlockToTop()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, 3, 1);

        Instruction(fingerprint, 'K')(context);

        CollectionAssert.AreEqual(new[] { 1, 5, 2, 3, 4 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void K_ReflectsWhenEndIsDeeperThanStart()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 1, 2);

        Instruction(fingerprint, 'K')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void N_ReversesTopNValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 3);

        Instruction(fingerprint, 'N')(context);

        CollectionAssert.AreEqual(new[] { 1, 4, 3, 2 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_PrintsBottomToTopNonDestructively()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        Instruction(fingerprint, 'P')(context);

        Assert.AreEqual("1 2 3 ", context.Output);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void R_ReversesEntireStack()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4);

        Instruction(fingerprint, 'R')(context);

        CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void S_DuplicatesSecondValue()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2);

        Instruction(fingerprint, 'S')(context);

        CollectionAssert.AreEqual(new[] { 1, 1, 2 }, PopAll(context));
    }

    [TestMethod]
    public void T_SwapsSecondAndThirdValues()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3);

        Instruction(fingerprint, 'T')(context);

        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, PopAll(context));
    }

    [TestMethod]
    public void U_DropsUntilValueIsFound()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 4, 2);

        Instruction(fingerprint, 'U')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 2 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void U_ReflectsWhenValueIsNotFound()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, 2, 3, 9);

        Instruction(fingerprint, 'U')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void W_ReflectsOnNegativeCount()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext(1, -1);
        context.PushVector(1, 0, 0);
        context.PushVector(0, 0, 0);

        Instruction(fingerprint, 'W')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void Z_ConvertsZeroStringToZeroGnirts()
    {
        var fingerprint = new StackManipulationFingerprint();
        var context = CreateContext('a', 'b', 'c', 0);

        Instruction(fingerprint, 'Z')(context);

        CollectionAssert.AreEqual(new[] { 0, 'a', 'b', 'c' }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }
}
