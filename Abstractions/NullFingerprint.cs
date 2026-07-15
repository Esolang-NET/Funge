using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge;

/// <summary>
/// Provides the standard Funge-98 <c>NULL</c> fingerprint (handprint <c>0x4E554C4C</c>).
/// All 26 instructions (<c>A</c>–<c>Z</c>) reflect the instruction pointer.
/// </summary>
/// <remarks>
/// <para>
/// The NULL fingerprint is defined in the Funge-98 specification as a fingerprint where
/// every instruction causes the IP to reflect. This is the canonical "no-op" fingerprint
/// used to verify that fingerprint loading and unloading work correctly.
/// </para>
/// <para>Use <see cref="Instance"/> to avoid allocating a new instance per use.</para>
/// </remarks>
/// <param name="name">Optional name for the fingerprint; defaults to <c>"NULL"</c>.</param>
public sealed class NullFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the NULL fingerprint, which is <c>"NULL"</c>.
    /// </summary>
    public const string NAME = "NULL";
    /// <summary>Gets the singleton instance of <see cref="NullFingerprint"/>.</summary>
    public static readonly NullFingerprint Instance = new();

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } = MakeBuilder().BuildInstructions();

    internal static IEnumerable<char> Keys => Enumerable.Range('A', 'Z' - 'A' + 1).Select(c => (char)c);
    static FingerprintBuilder MakeBuilder() => Keys.Aggregate(new FingerprintBuilder(), AddInstruction);
    static FingerprintBuilder AddInstruction(FingerprintBuilder builder, char c)
    {
        builder.Add(c, Value);
        return builder;
    }
    static void Value(IFungeExecutionContext ctx) => ctx.Reflect();
}
