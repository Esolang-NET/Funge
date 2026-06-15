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

public class ThreeDeeSpaceFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(ThreeDeeSpaceFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static int SingleToInt32Bits(float value)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        return BitConverter.SingleToInt32Bits(value);
#else
        var bytes = BitConverter.GetBytes(value);
        return BitConverter.ToInt32(bytes, 0);
#endif
    }

    static float Int32BitsToSingle(int bits)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        return BitConverter.Int32BitsToSingle(bits);
#else
        var bytes = BitConverter.GetBytes(bits);
        return BitConverter.ToSingle(bytes, 0);
#endif
    }

    static void PushFloat(TestContext ctx, float value)
        => ctx.Push(SingleToInt32Bits(value));

    static float PopFloat(TestContext ctx)
        => Int32BitsToSingle(ctx.Pop());

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

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new ThreeDeeSpaceFingerprint().Handprint).IsEqualTo(0x33445350);

    [Test]
    public async Task Instruction_A_AddVectors()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        PushVec3(ctx, 4f, 5f, 6f);
        (await Instruction(fp, 'A'))(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        await Assert.That(rX).IsEqualTo(5f).Within(0.0001f);
        await Assert.That(rY).IsEqualTo(7f).Within(0.0001f);
        await Assert.That(rZ).IsEqualTo(9f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_B_SubtractVectors()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 5f, 7f, 9f);
        PushVec3(ctx, 1f, 2f, 3f);
        (await Instruction(fp, 'B'))(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        await Assert.That(rX).IsEqualTo(4f).Within(0.0001f);
        await Assert.That(rY).IsEqualTo(5f).Within(0.0001f);
        await Assert.That(rZ).IsEqualTo(6f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_D_DotProduct()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 0f, 0f);
        PushVec3(ctx, 1f, 0f, 0f);
        (await Instruction(fp, 'D'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(1.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_L_Length()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 3f, 4f, 0f);
        (await Instruction(fp, 'L'))(ctx);
        await Assert.That(PopFloat(ctx)).IsEqualTo(5f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_N_Normalize()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 3f, 0f, 0f);
        (await Instruction(fp, 'N'))(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        await Assert.That(rX).IsEqualTo(1.0f).Within(0.0001f);
        await Assert.That(rY).IsEqualTo(0.0f).Within(0.0001f);
        await Assert.That(rZ).IsEqualTo(0.0f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_U_Duplicate()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        (await Instruction(fp, 'U'))(ctx);
        var (v1X, _, _) = PopVec3(ctx);
        var (v2X, _, _) = PopVec3(ctx);
        await Assert.That(v1X).IsEqualTo(1f).Within(0.0001f);
        await Assert.That(v2X).IsEqualTo(1f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_Z_Scale()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 2f, 3f);
        PushFloat(ctx, 2f);
        (await Instruction(fp, 'Z'))(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        await Assert.That(rX).IsEqualTo(2f).Within(0.0001f);
        await Assert.That(rY).IsEqualTo(4f).Within(0.0001f);
        await Assert.That(rZ).IsEqualTo(6f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_C_CrossProduct()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new TestContext();
        PushVec3(ctx, 1f, 0f, 0f); // a
        PushVec3(ctx, 0f, 1f, 0f); // b
        (await Instruction(fp, 'C'))(ctx);
        var (rX, rY, rZ) = PopVec3(ctx);
        // (1,0,0) x (0,1,0) = (0,0,1)
        await Assert.That(rX).IsEqualTo(0f).Within(0.0001f);
        await Assert.That(rY).IsEqualTo(0f).Within(0.0001f);
        await Assert.That(rZ).IsEqualTo(1f).Within(0.0001f);
        await Assert.That(ctx.Reflected).IsFalse();
    }

    [Test]
    public async Task Instruction_P_NoContexts_Reflects()
    {
        var fp = new ThreeDeeSpaceFingerprint();
        var ctx = new BasicContext();
        (await Instruction(fp, 'P'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
