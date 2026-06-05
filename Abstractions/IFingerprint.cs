namespace Esolang.Funge;

/// <summary>
/// Represents a Funge-98 fingerprint (extension library) that provides
/// custom instruction semantics for letters <c>A</c>–<c>Z</c>.
/// </summary>
public interface IFingerprint
{
    /// <summary>
    /// Gets the unique 32-bit handprint that identifies this fingerprint.
    /// Computed as the ASCII codes of the fingerprint name shifted into successive bytes
    /// (e.g., "NULL" → <c>0x4E554C4C</c>).
    /// </summary>
    int Handprint { get; }

    /// <summary>
    /// Gets the mapping of uppercase letter instructions (<c>'A'</c>–<c>'Z'</c>)
    /// to their handler delegates.
    /// </summary>
    IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }
}
