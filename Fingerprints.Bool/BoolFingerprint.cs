using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Bool;

/// <summary>
/// Provides the standard Funge-98 <c>BOOL</c> fingerprint (handprint <c>0x424F4F4C</c>).
/// </summary>
public sealed class BoolFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"BOOL"</c>.
    /// </summary>
    public const string NAME = "BOOL";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('A', And)
            .Add('N', Not)
            .Add('O', Or)
            .Add('X', Xor)
            .BuildInstructions();

    static void And(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push((a != 0 && b != 0) ? 1 : 0);
    }

    static void Not(IFungeExecutionContext ctx)
    {
        var a = ctx.Pop();
        ctx.Push(a == 0 ? 1 : 0);
    }

    static void Or(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push((a != 0 || b != 0) ? 1 : 0);
    }

    static void Xor(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(((a != 0) ^ (b != 0)) ? 1 : 0);
    }
}
