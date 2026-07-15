using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Roma;

/// <summary>
/// Provides the standard Funge-98 <c>ROMA</c> fingerprint (handprint <c>0x524F4D41</c>).
/// </summary>
public sealed class RomanFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"ROMA"</c>.
    /// </summary>
    public const string NAME = "ROMA";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('C', static ctx => ctx.Push(100))
            .Add('D', static ctx => ctx.Push(500))
            .Add('I', static ctx => ctx.Push(1))
            .Add('L', static ctx => ctx.Push(50))
            .Add('M', static ctx => ctx.Push(1000))
            .Add('V', static ctx => ctx.Push(5))
            .Add('X', static ctx => ctx.Push(10))
            .BuildInstructions();
}
