namespace Esolang.Funge;

/// <summary>
/// Provides vector stack operations for fingerprint instructions.
/// </summary>
public interface IFungeVectorContext
{
    /// <summary>Pops a vector from the stack in (x, y, z) order.</summary>
    (int X, int Y, int Z) PopVector();

    /// <summary>Pushes a vector onto the stack in (x, y, z) order.</summary>
    void PushVector(int x, int y, int z);
}
