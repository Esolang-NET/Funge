namespace Esolang.Funge;

/// <summary>
/// Provides access to the runtime random-number source used by the current execution.
/// </summary>
public interface IFungeRandomContext
{
    /// <summary>Returns a random unsigned integer in the range <c>[0, exclusiveUpperBound)</c>.</summary>
    uint NextUInt32(uint exclusiveUpperBound);

    /// <summary>Returns a random single-precision floating-point value in the range <c>[0, 1)</c>.</summary>
    float NextSingle();

    /// <summary>Reseeds the shared random-number source using the supplied seed.</summary>
    void Reseed(uint seed);

    /// <summary>Reseeds the shared random-number source using a time-based or implementation-defined seed.</summary>
    void Reseed();
}
