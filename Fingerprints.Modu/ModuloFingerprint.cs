namespace Esolang.Funge.Fingerprints.Modu;

/// <summary>
/// Provides the standard Funge-98 <c>MODU</c> fingerprint (handprint <c>0x4D4F4455</c>).
/// </summary>
public sealed class ModuloFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("MODU");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="ModuloFingerprint"/>.</summary>
    public ModuloFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('M', SignedResultModulo)
            .Add('R', Remainder)
            .Add('U', UnsignedResultModulo)
            .BuildInstructions();

    static void SignedResultModulo(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        if (b == 0)
        {
            ctx.Push(0);
            return;
        }
        var r = a % b;
        ctx.Push((r != 0 && (r ^ b) < 0) ? r + b : r);
    }

    static void Remainder(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(b == 0 ? 0 : a % b);
    }

    static void UnsignedResultModulo(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        if (b == 0)
        {
            ctx.Push(0);
            return;
        }
        var r = a % b;
        ctx.Push(r < 0 ? r + Math.Abs(b) : r);
    }
}
