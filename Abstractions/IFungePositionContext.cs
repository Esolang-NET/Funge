namespace Esolang.Funge;

/// <summary>
/// Provides access to the current instruction pointer's position and delta in funge-space.
/// </summary>
public interface IFungePositionContext
{
    /// <summary>Gets or sets the current position of the instruction pointer.</summary>
    (int X, int Y, int Z) Position { get; set; }

    /// <summary>Gets or sets the current delta (direction) of the instruction pointer.</summary>
    (int X, int Y, int Z) Delta { get; set; }
}
