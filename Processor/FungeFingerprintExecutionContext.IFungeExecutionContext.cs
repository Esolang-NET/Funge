namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeExecutionContext
{
    /// <inheritdoc/>
    public void Push(int value) => ExecutionContext.Push(value);

    /// <inheritdoc/>
    public int Pop() => ExecutionContext.Pop();

    /// <inheritdoc/>
    public int Peek() => ExecutionContext.Peek();

    /// <inheritdoc/>
    public void Reflect() => ExecutionContext.Reflect();
}
