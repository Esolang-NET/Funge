namespace Esolang.Funge;

/// <summary>
/// Receives instruction-pointer lifecycle notifications from runtimes that support
/// per-IP fingerprint state management.
/// </summary>
public interface IFungeInstructionPointerLifecycle
{
    /// <summary>
    /// Called when an instruction pointer is cloned by <c>t</c>.
    /// </summary>
    /// <param name="parentInstructionPointerId">The parent instruction pointer identifier.</param>
    /// <param name="childInstructionPointerId">The new child instruction pointer identifier.</param>
    void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId);

    /// <summary>
    /// Called when an instruction pointer terminates or is otherwise removed from execution.
    /// </summary>
    /// <param name="instructionPointerId">The terminated instruction pointer identifier.</param>
    void OnInstructionPointerTerminated(int instructionPointerId);
}
