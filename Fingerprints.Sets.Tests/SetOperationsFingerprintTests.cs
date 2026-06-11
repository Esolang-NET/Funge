namespace Esolang.Funge.Fingerprints.Sets;

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
public class SetOperationsFingerprintTests
{
    static FingerprintInstruction Instruction(SetOperationsFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushSet(TestContext ctx, params int[] elements)
    {
        foreach (var e in elements)
            ctx.Push(e);
        ctx.Push(elements.Length);
    }

    static HashSet<int> PopSet(TestContext ctx)
    {
        var count = ctx.Pop();
        var set = new HashSet<int>();
        for (var i = 0; i < count; i++)
            set.Add(ctx.Pop());
        return set;
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x53455453, new SetOperationsFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_AddElement()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        ctx.Push(4);
        Instruction(fp, 'A')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 1, 2, 3, 4 }));
    }

    [TestMethod]
    public void Instruction_A_AddExisting_NoChange()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        ctx.Push(2);
        Instruction(fp, 'A')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 1, 2 }));
    }

    [TestMethod]
    public void Instruction_U_Union()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        PushSet(ctx, 2, 3);
        Instruction(fp, 'U')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 1, 2, 3 }));
    }

    [TestMethod]
    public void Instruction_I_Intersect()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        PushSet(ctx, 2, 3, 4);
        Instruction(fp, 'I')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 2, 3 }));
    }

    [TestMethod]
    public void Instruction_S_Subtract()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        PushSet(ctx, 2, 3);
        Instruction(fp, 'S')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 1 }));
    }

    [TestMethod]
    public void Instruction_R_Remove()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        ctx.Push(2);
        Instruction(fp, 'R')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var result = PopSet(ctx);
        Assert.IsTrue(result.SetEquals(new HashSet<int> { 1, 3 }));
    }

    [TestMethod]
    public void Instruction_M_Member_Found()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        ctx.Push(2);
        PushSet(ctx, 1, 2, 3);
        Instruction(fp, 'M')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(1, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_M_Member_NotFound()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        ctx.Push(99);
        PushSet(ctx, 1, 2, 3);
        Instruction(fp, 'M')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(0, ctx.Pop());
    }

    [TestMethod]
    public void Instruction_Z_Discard()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2, 3);
        Instruction(fp, 'Z')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.AreEqual(0, ctx.Pop()); // stack empty
    }

    [TestMethod]
    public void Instruction_D_Duplicate()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 5, 6);
        Instruction(fp, 'D')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var first = PopSet(ctx);
        var second = PopSet(ctx);
        Assert.IsTrue(first.SetEquals(new HashSet<int> { 5, 6 }));
        Assert.IsTrue(second.SetEquals(new HashSet<int> { 5, 6 }));
    }

    [TestMethod]
    public void Instruction_X_Exchange()
    {
        var fp = new SetOperationsFingerprint();
        var ctx = new TestContext();
        PushSet(ctx, 1, 2);
        PushSet(ctx, 3, 4);
        Instruction(fp, 'X')(ctx);
        Assert.IsFalse(ctx.Reflected);
        var top = PopSet(ctx);
        var bottom = PopSet(ctx);
        Assert.IsTrue(top.SetEquals(new HashSet<int> { 1, 2 }));
        Assert.IsTrue(bottom.SetEquals(new HashSet<int> { 3, 4 }));
    }
}
