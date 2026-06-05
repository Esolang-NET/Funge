namespace Esolang.Funge.Fingerprints.Frth;

sealed class TestContext : IFungeExecutionContext, IFungeStackContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; private set; }

    public int StackDepth => _stack.Count;

    public void Push(int value) => _stack.Push(value);

    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;

    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;

    public void Reflect() => Reflected = true;
}

[TestClass]
public class ForthFingerprintTests
{
    static FingerprintInstruction Instruction(ForthFingerprint fingerprint, char instruction)
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
        => Assert.AreEqual(0x46525448, new ForthFingerprint().Handprint);

    [TestMethod]
    public void D_PushesStackDepth()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3);

        Instruction(fingerprint, 'D')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 3 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void O_PerformsForthOver()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2);

        Instruction(fingerprint, 'O')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 1 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void R_PerformsForthRot()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3);

        Instruction(fingerprint, 'R')(context);

        CollectionAssert.AreEqual(new[] { 2, 3, 1 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_PicksIndexedValue()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 0);

        Instruction(fingerprint, 'P')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 3 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_PushesZeroWhenIndexIsTooDeep()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 5);

        Instruction(fingerprint, 'P')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void P_ReflectsOnNegativeIndex()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, -1);

        Instruction(fingerprint, 'P')(context);

        Assert.IsTrue(context.Reflected);
    }

    [TestMethod]
    public void L_RollsIndexedValueToTop()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, 3);

        Instruction(fingerprint, 'L')(context);

        CollectionAssert.AreEqual(new[] { 1, 3, 4, 5, 2 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void L_WithPositiveIndexPastDepth_PushesZero()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 5);

        Instruction(fingerprint, 'L')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void L_WithNegativeIndexMovesTopDeeper()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, 4, 5, -3);

        Instruction(fingerprint, 'L')(context);

        CollectionAssert.AreEqual(new[] { 1, 2, 5, 3, 4 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }

    [TestMethod]
    public void L_WithNegativeIndexBeyondDepth_PadsWithZeroes()
    {
        var fingerprint = new ForthFingerprint();
        var context = CreateContext(1, 2, 3, -5);

        Instruction(fingerprint, 'L')(context);

        CollectionAssert.AreEqual(new[] { 3, 0, 0, 1, 2 }, PopAll(context));
        Assert.IsFalse(context.Reflected);
    }
}
