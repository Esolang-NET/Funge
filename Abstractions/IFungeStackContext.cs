namespace Esolang.Funge;

/// <summary>
/// Provides information about the current top stack for fingerprints that need stack-shape awareness.
/// </summary>
public interface IFungeStackContext
{
    /// <summary>Gets the number of items currently on the top stack.</summary>
    int StackDepth { get; }
}
