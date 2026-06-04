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
public sealed class NullFingerprint : IFingerprint
{
    /// <summary>Gets the singleton instance of <see cref="NullFingerprint"/>.</summary>
    public static readonly NullFingerprint Instance = new();

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("NULL");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } =
        new FingerprintBuilder()
            .Add('A', static ctx => ctx.Reflect())
            .Add('B', static ctx => ctx.Reflect())
            .Add('C', static ctx => ctx.Reflect())
            .Add('D', static ctx => ctx.Reflect())
            .Add('E', static ctx => ctx.Reflect())
            .Add('F', static ctx => ctx.Reflect())
            .Add('G', static ctx => ctx.Reflect())
            .Add('H', static ctx => ctx.Reflect())
            .Add('I', static ctx => ctx.Reflect())
            .Add('J', static ctx => ctx.Reflect())
            .Add('K', static ctx => ctx.Reflect())
            .Add('L', static ctx => ctx.Reflect())
            .Add('M', static ctx => ctx.Reflect())
            .Add('N', static ctx => ctx.Reflect())
            .Add('O', static ctx => ctx.Reflect())
            .Add('P', static ctx => ctx.Reflect())
            .Add('Q', static ctx => ctx.Reflect())
            .Add('R', static ctx => ctx.Reflect())
            .Add('S', static ctx => ctx.Reflect())
            .Add('T', static ctx => ctx.Reflect())
            .Add('U', static ctx => ctx.Reflect())
            .Add('V', static ctx => ctx.Reflect())
            .Add('W', static ctx => ctx.Reflect())
            .Add('X', static ctx => ctx.Reflect())
            .Add('Y', static ctx => ctx.Reflect())
            .Add('Z', static ctx => ctx.Reflect())
            .BuildInstructions();
}
