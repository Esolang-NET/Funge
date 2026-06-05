using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor;

/// <summary>
/// Wraps an <see cref="InstructionPointer"/> to implement <see cref="IFungeExecutionContext"/>
/// for use by fingerprint instruction handlers.
/// </summary>
abstract class FungeExecutionContext(InstructionPointer ip, FungeSpace space)
    : IFungeExecutionContext, IFungeInstructionPointerContext, IFungeStackContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext
{
    public static IFungeExecutionContext Create(InstructionPointer ip, FungeSpace space, TextReader? input, TextWriter? output)
    {
        if (input is not null && output is not null)
            return new FungeIoExecutionContext(ip, space, input, output);
        if (input is not null)
            return new FungeInputExecutionContext(ip, space, input);
        if (output is not null)
            return new FungeOutputExecutionContext(ip, space, output);
        return new FungeCoreExecutionContext(ip, space);
    }

    /// <inheritdoc/>
    public void Push(int value) => ip.StackStack.Push(value);

    /// <inheritdoc/>
    public int Pop() => ip.StackStack.Pop();

    /// <inheritdoc/>
    public int Peek() => ip.StackStack.Peek();

    /// <inheritdoc/>
    public int InstructionPointerId => ip.Id;

    /// <inheritdoc/>
    public int StackDepth => ip.StackStack.TOSS.Count;

    /// <inheritdoc/>
    public (int X, int Y, int Z) PopVector()
    {
        var z = ip.StackStack.Pop();
        var y = ip.StackStack.Pop();
        var x = ip.StackStack.Pop();
        return (x, y, z);
    }

    /// <inheritdoc/>
    public void PushVector(int x, int y, int z)
    {
        ip.StackStack.Push(x);
        ip.StackStack.Push(y);
        ip.StackStack.Push(z);
    }

    /// <inheritdoc/>
    public int Dimensions => 3;

    /// <inheritdoc/>
    public (int X, int Y, int Z) StorageOffset => (ip.Offset.X, ip.Offset.Y, ip.Offset.Z);

    /// <inheritdoc/>
    public int GetCell(int x, int y, int z) => space[new FungeVector(x, y, z)];

    /// <inheritdoc/>
    public void SetCell(int x, int y, int z, int value) => space[new FungeVector(x, y, z)] = value;

    /// <inheritdoc/>
    public void Reflect() => ip.Delta = ip.Delta.Reflect();
}

sealed class FungeCoreExecutionContext(InstructionPointer ip, FungeSpace space) : FungeExecutionContext(ip, space);

sealed class FungeInputExecutionContext(InstructionPointer ip, FungeSpace space, TextReader input)
    : FungeExecutionContext(ip, space), IFungeInputContext
{
    public string? ReadLine() => input.ReadLine();
}

sealed class FungeOutputExecutionContext(InstructionPointer ip, FungeSpace space, TextWriter output)
    : FungeExecutionContext(ip, space), IFungeOutputContext
{
    public void WriteString(string value) => output.Write(value);
}

sealed class FungeIoExecutionContext(InstructionPointer ip, FungeSpace space, TextReader input, TextWriter output)
    : FungeExecutionContext(ip, space), IFungeInputContext, IFungeOutputContext
{
    public string? ReadLine() => input.ReadLine();
    public void WriteString(string value) => output.Write(value);
}
