namespace Esolang.Funge;

/// <summary>
/// Provides absolute funge-space access for fingerprint instructions.
/// </summary>
public interface IFungeSpaceContext
{
    /// <summary>Gets the maximum supported dimensionality for vector-based space operations.</summary>
    int Dimensions { get; }

    /// <summary>Reads a cell from absolute funge-space coordinates.</summary>
    int GetCell(int x, int y, int z);

    /// <summary>Writes a cell to absolute funge-space coordinates.</summary>
    void SetCell(int x, int y, int z, int value);
}
