using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Roma;

sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;
}

public class RomanFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(RomanFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsCorrect()
        => await Assert.That(new RomanFingerprint().Handprint).IsEqualTo(0x524F4D41);

    [Test]
    [Arguments('I', 1)]
    [Arguments('V', 5)]
    [Arguments('X', 10)]
    [Arguments('L', 50)]
    [Arguments('C', 100)]
    [Arguments('D', 500)]
    [Arguments('M', 1000)]
    public async Task Instructions_PushCorrectValues(char ch, int expected)
    {
        var fp = new RomanFingerprint();
        var ctx = new TestContext();
        await (await Instruction(fp, ch))(ctx);
        await Assert.That(ctx.Pop()).IsEqualTo(expected);
        await Assert.That(ctx.Reflected).IsFalse();
    }
}
