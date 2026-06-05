namespace Esolang.Funge.Fingerprints.Bool;

/// <summary>
/// Provides the standard Funge-98 <c>BOOL</c> fingerprint (handprint <c>0x424F4F4C</c>).
/// </summary>
public sealed class BoolFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("BOOL");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="BoolFingerprint"/>.</summary>
    public BoolFingerprint() =>
        Instructions = new FingerprintBuilder()
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
