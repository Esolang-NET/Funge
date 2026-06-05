namespace Esolang.Funge;

/// <summary>
/// Provides the current storage offset for fingerprints that use offset-relative vectors.
/// </summary>
public interface IFungeStorageOffsetContext
{
    /// <summary>Gets the current storage offset in (x, y, z) order.</summary>
    (int X, int Y, int Z) StorageOffset { get; }
}
