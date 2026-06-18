using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Roma;

/// <summary>
/// Provides the standard Funge-98 <c>ROMA</c> fingerprint (handprint <c>0x524F4D41</c>).
/// </summary>
public sealed class RomanFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("ROMA");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="RomanFingerprint"/>.</summary>
    public RomanFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('C', static ctx => ctx.Push(100))
            .Add('D', static ctx => ctx.Push(500))
            .Add('I', static ctx => ctx.Push(1))
            .Add('L', static ctx => ctx.Push(50))
            .Add('M', static ctx => ctx.Push(1000))
            .Add('V', static ctx => ctx.Push(5))
            .Add('X', static ctx => ctx.Push(10))
            .BuildInstructions();
}
