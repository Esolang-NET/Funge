namespace Esolang.Funge.Fingerprints.Dirf;

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
public class DirectoryFingerprintTests
{
    static FingerprintInstruction Instruction(DirectoryFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    static void Push0gnirts(TestContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    [TestMethod]
    public void Handprint_IsCorrect()
        => Assert.AreEqual(0x44495246, new DirectoryFingerprint().Handprint);

    [TestMethod]
    public void Instruction_M_CreateDirectory_Succeeds()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var path = Path.Combine(Path.GetTempPath(), "dirf_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            Push0gnirts(ctx, path);
            Instruction(fp, 'M')(ctx);
            Assert.IsFalse(ctx.Reflected);
            Assert.IsTrue(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
        }
    }

    [TestMethod]
    public void Instruction_M_InvalidPath_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        ctx.Push(0); // empty path — Directory.CreateDirectory("") throws ArgumentException
        Instruction(fp, 'M')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_R_RemoveDirectory_Succeeds()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var path = Path.Combine(Path.GetTempPath(), "dirf_remove_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        Push0gnirts(ctx, path);
        Instruction(fp, 'R')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.IsFalse(Directory.Exists(path));
    }

    [TestMethod]
    public void Instruction_R_NonExistent_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, Path.Combine(Path.GetTempPath(), "dirf_nonexistent_" + Guid.NewGuid().ToString("N")));
        Instruction(fp, 'R')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    [TestMethod]
    public void Instruction_C_ChangesDirectory()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var original = Directory.GetCurrentDirectory();
        var path = Path.GetTempPath();
        try
        {
            Push0gnirts(ctx, path);
            Instruction(fp, 'C')(ctx);
            Assert.IsFalse(ctx.Reflected);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    [TestMethod]
    public void Instruction_C_InvalidPath_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "\0invalid");
        Instruction(fp, 'C')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }
}
