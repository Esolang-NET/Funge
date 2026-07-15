using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Modu;

/// <summary>
/// Provides the standard Funge-98 <c>MODU</c> fingerprint (handprint <c>0x4D4F4455</c>).
/// </summary>
public sealed class ModuloFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"MODU"</c>.
    /// </summary>
    public const string NAME = "MODU";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
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
