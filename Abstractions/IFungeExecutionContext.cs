namespace Esolang.Funge;

/// <summary>
/// Provides access to the stack and control flow of the current instruction pointer
/// during fingerprint instruction execution.
/// </summary>
public interface IFungeExecutionContext
{
    /// <summary>Pushes a value onto the top-of-stack-stack.</summary>
    void Push(int value);

    /// <summary>Pops a value from the top-of-stack-stack. Returns <c>0</c> if the stack is empty.</summary>
    int Pop();

    /// <summary>Peeks at the top of the stack without removing it. Returns <c>0</c> if empty.</summary>
    int Peek();

    /// <summary>Reflects the instruction pointer (reverses its delta direction).</summary>
    void Reflect();
}
