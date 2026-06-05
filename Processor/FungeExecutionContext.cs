namespace Esolang.Funge.Processor;

/// <summary>
/// Wraps an <see cref="InstructionPointer"/> to implement <see cref="IFungeExecutionContext"/>
/// for use by fingerprint instruction handlers.
/// </summary>
abstract class FungeExecutionContext(InstructionPointer ip) : IFungeExecutionContext
{
    public static IFungeExecutionContext Create(InstructionPointer ip, TextReader? input, TextWriter? output)
    {
        if (input is not null && output is not null)
            return new FungeIoExecutionContext(ip, input, output);
        if (input is not null)
            return new FungeInputExecutionContext(ip, input);
        if (output is not null)
            return new FungeOutputExecutionContext(ip, output);
        return new FungeCoreExecutionContext(ip);
    }

    /// <inheritdoc/>
    public void Push(int value) => ip.StackStack.Push(value);

    /// <inheritdoc/>
    public int Pop() => ip.StackStack.Pop();

    /// <inheritdoc/>
    public int Peek() => ip.StackStack.Peek();

    /// <inheritdoc/>
    public void Reflect() => ip.Delta = ip.Delta.Reflect();
}

sealed class FungeCoreExecutionContext(InstructionPointer ip) : FungeExecutionContext(ip);

sealed class FungeInputExecutionContext(InstructionPointer ip, TextReader input)
    : FungeExecutionContext(ip), IFungeInputContext
{
    public string? ReadLine() => input.ReadLine();
}

sealed class FungeOutputExecutionContext(InstructionPointer ip, TextWriter output)
    : FungeExecutionContext(ip), IFungeOutputContext
{
    public void WriteString(string value) => output.Write(value);
}

sealed class FungeIoExecutionContext(InstructionPointer ip, TextReader input, TextWriter output)
    : FungeExecutionContext(ip), IFungeInputContext, IFungeOutputContext
{
    public string? ReadLine() => input.ReadLine();
    public void WriteString(string value) => output.Write(value);
}
