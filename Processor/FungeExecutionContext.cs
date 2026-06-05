namespace Esolang.Funge.Processor;

/// <summary>
/// Wraps an <see cref="InstructionPointer"/> to implement <see cref="IFungeExecutionContext"/>
/// for use by fingerprint instruction handlers.
/// </summary>
sealed class FungeExecutionContext(InstructionPointer ip, TextReader? input, TextWriter? output) : IFungeExecutionContext
{
    /// <inheritdoc/>
    public void Push(int value) => ip.StackStack.Push(value);

    /// <inheritdoc/>
    public int Pop() => ip.StackStack.Pop();

    /// <inheritdoc/>
    public int Peek() => ip.StackStack.Peek();

    /// <inheritdoc/>
    public void WriteString(string value)
    {
        if (output is null)
            throw new InvalidOperationException("Fingerprint output requires a TextWriter.");

        output.Write(value);
    }

    /// <inheritdoc/>
    public string? ReadLine()
    {
        if (input is null)
            throw new InvalidOperationException("Fingerprint input requires a TextReader.");

        return input.ReadLine();
    }

    /// <inheritdoc/>
    public void Reflect() => ip.Delta = ip.Delta.Reflect();
}
