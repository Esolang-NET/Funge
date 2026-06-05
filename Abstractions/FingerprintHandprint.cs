namespace Esolang.Funge;

/// <summary>
/// Provides helpers for computing Funge-98 fingerprint handprints.
/// </summary>
/// <remarks>
/// A handprint is the 32-bit integer formed by taking each ASCII character of the
/// fingerprint name and shifting it into successive bytes.
/// For example, <c>"NULL"</c> → <c>0x4E554C4C</c>.
/// </remarks>
public static class FingerprintHandprint
{
    /// <summary>
    /// Computes the handprint for the given fingerprint name string.
    /// Each character is masked to its low byte and shifted left by 8 bits.
    /// </summary>
    /// <param name="name">The fingerprint name, e.g. <c>"NULL"</c> or <c>"STRN"</c>.</param>
    /// <returns>The 32-bit handprint integer.</returns>
    public static int Compute(string name)
    {
        var result = 0;
        foreach (var c in name)
            result = (result << 8) | (c & 0xFF);
        return result;
    }
}
