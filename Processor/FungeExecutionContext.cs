namespace Esolang.Funge.Processor;

/// <summary>
/// Wraps an <see cref="InstructionPointer"/> to implement <see cref="IFungeExecutionContext"/>
/// for use by fingerprint instruction handlers.
/// </summary>
sealed class FungeExecutionContext(InstructionPointer ip) : IFungeExecutionContext
{
    /// <inheritdoc/>
    public void Push(int value) => ip.StackStack.Push(value);

    /// <inheritdoc/>
    public int Pop() => ip.StackStack.Pop();

    /// <inheritdoc/>
    public int Peek() => ip.StackStack.Peek();

    /// <inheritdoc/>
    public void Reflect() => ip.Delta = ip.Delta.Reflect();
}
