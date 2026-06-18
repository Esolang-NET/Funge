using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
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

public class DirectoryFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(DirectoryFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    static void Push0gnirts(TestContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new DirectoryFingerprint().Handprint).IsEqualTo(0x44495246);

    [Test]
    public async Task Instruction_M_CreateDirectory_Succeeds()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var path = Path.Combine(Path.GetTempPath(), "dirf_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            Push0gnirts(ctx, path);
            await (await Instruction(fp, 'M'))(ctx);
            await Assert.That(ctx.Reflected).IsFalse();
            await Assert.That(Directory.Exists(path)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
        }
    }

    [Test]
    public async Task Instruction_M_InvalidPath_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        ctx.Push(0); // empty path — Directory.CreateDirectory("") throws ArgumentException
        await (await Instruction(fp, 'M'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_R_RemoveDirectory_Succeeds()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var path = Path.Combine(Path.GetTempPath(), "dirf_remove_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        Push0gnirts(ctx, path);
        await (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(Directory.Exists(path)).IsFalse();
    }

    [Test]
    public async Task Instruction_R_NonExistent_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, Path.Combine(Path.GetTempPath(), "dirf_nonexistent_" + Guid.NewGuid().ToString("N")));
        await (await Instruction(fp, 'R'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Instruction_C_ChangesDirectory()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        var original = Directory.GetCurrentDirectory();
        var path = Path.GetTempPath();
        try
        {
            Push0gnirts(ctx, path);
            await (await Instruction(fp, 'C'))(ctx);
            await Assert.That(ctx.Reflected).IsFalse();
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    [Test]
    public async Task Instruction_C_InvalidPath_Reflects()
    {
        var fp = new DirectoryFingerprint();
        var ctx = new TestContext();
        Push0gnirts(ctx, "\0invalid");
        await (await Instruction(fp, 'C'))(ctx);
        await Assert.That(ctx.Reflected).IsTrue();
    }
}
