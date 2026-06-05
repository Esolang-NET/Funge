namespace Esolang.Funge;

/// <summary>
/// Provides information about the currently executing instruction pointer.
/// </summary>
public interface IFungeInstructionPointerContext
{
    /// <summary>Gets the unique identifier of the current instruction pointer.</summary>
    int InstructionPointerId { get; }
}
