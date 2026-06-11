namespace Esolang.Funge.Fingerprints.ThreeDsp;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int, int, int), int> _space = [];
    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public int Dimensions => 3;
    public int GetCell(int x, int y, int z) => _space.TryGetValue((x, y, z), out var v) ? v : 0;
    public void SetCell(int x, int y, int z, int value) => _space[(x, y, z)] = value;

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop(); var y = Pop(); var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z) { Push(x); Push(y); Push(z); }
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
public class ThreeDeeSpaceFingerprintTests
{
    static FingerprintInstruction Instruction(ThreeDeeSpaceFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void PushFloat(TestContext ctx, float value)
        => ctx.Push(BitConverter.SingleToInt32Bits(value));

    static float PopFloat(TestContext ctx)
        => BitConverter.Int32BitsToSingle(ctx.Pop());

    static void PushVec3(TestContext ctx, float x, float y, float z)
    {
        PushFloat(ctx, x);
        PushFloat(ctx, y);
        PushFloat(ctx, z);
    }

    static (float X, float Y, float Z) PopVec3(TestContext ctx)
    {
        var z = PopFloat(ctx);
        var y = PopFloat(ctx);
        var x = PopFloat(ctx);
        return (x, y, z);
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x33445350, new ThreeDeeSpaceFingerprint().Handprint);

    [TestMethod]
    public void Instruction_A_AddVectors()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        PushVec3(ctx, 4f, 5f, 6f);
        Instruction(fp, 'A')(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        Assert.AreEqual(5f, rX, 0.0001f);
        Assert.AreEqual(7f, rY, 0.0001f);
        Assert.AreEqual(9f, rZ, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_B_SubtractVectors()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 5f, 7f, 9f);
        PushVec3(ctx, 1f, 2f, 3f);
        Instruction(fp, 'B')(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        Assert.AreEqual(4f, rX, 0.0001f);
        Assert.AreEqual(5f, rY, 0.0001f);
        Assert.AreEqual(6f, rZ, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_D_DotProduct()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 0f, 0f);
        PushVec3(ctx, 1f, 0f, 0f);
        Instruction(fp, 'D')(ctx);
        Assert.AreEqual(1.0f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_L_Length()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 3f, 4f, 0f);
        Instruction(fp, 'L')(ctx);
        Assert.AreEqual(5f, PopFloat(ctx), 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_N_Normalize()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 3f, 0f, 0f);
        Instruction(fp, 'N')(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        Assert.AreEqual(1.0f, rX, 0.0001f);
        Assert.AreEqual(0.0f, rY, 0.0001f);
        Assert.AreEqual(0.0f, rZ, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_U_Duplicate()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        Instruction(fp, 'U')(ctx);
        var (v1X, _, _) = PopVec3(ctx);
        var (v2X, _, _) = PopVec3(ctx);
        Assert.AreEqual(1f, v1X, 0.0001f);
        Assert.AreEqual(1f, v2X, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_Z_Scale()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        PushFloat(ctx, 2f);
        Instruction(fp, 'Z')(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        Assert.AreEqual(2f, rX, 0.0001f);
        Assert.AreEqual(4f, rY, 0.0001f);
        Assert.AreEqual(6f, rZ, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_C_CrossProduct()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 0f, 0f); // a
        PushVec3(ctx, 0f, 1f, 0f); // b
        Instruction(fp, 'C')(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        // (1,0,0) x (0,1,0) = (0,0,1)
        Assert.AreEqual(0f, rX, 0.0001f);
        Assert.AreEqual(0f, rY, 0.0001f);
        Assert.AreEqual(1f, rZ, 0.0001f);
        Assert.IsFalse(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_P_NoContexts_Reflects()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new BasicContext();
        Instruction(fp, 'P')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
