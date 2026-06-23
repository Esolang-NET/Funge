using Esolang.Funge.Parser;
using Esolang.Processor;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

/// <summary>
/// Wraps an <see cref="InstructionPointer"/> to implement <see cref="IFungeExecutionContext"/>
/// for use by fingerprint instruction handlers.
/// </summary>
class FungeExecutionContext(InstructionPointer ip, FungeSpace space, FingerprintInstruction function, FungeRandomSource random)
    : IFungeExecutionContext, IFungeInstructionPointerContext, IFungeStackContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext, IFungeRandomContext
{
    public static FungeExecutionContext Create(InstructionPointer ip, FungeSpace space, bool enableInput, bool enableOutput, FingerprintInstruction function, FungeRandomSource random)
    {
        if (enableInput is true && enableOutput is true)
            return new FungeIoExecutionContext(ip, space, function, random);
        if (enableInput is true)
            return new FungeInputExecutionContext(ip, space, function, random);
        if (enableOutput is true)
            return new FungeOutputExecutionContext(ip, space, function, random);
        return new FungeCoreExecutionContext(ip, space, function, random);
    }

    readonly List<TaskCompletionSource<IOEvent>> ioEventRequestWaiters = [new TaskCompletionSource<IOEvent>()];
    readonly List<TaskCompletionSource> ioEventResponseWaiters = [];
    protected Task Emit(IOEvent ioEvent)
    {
        var last = ioEventRequestWaiters[^1];
        ioEventRequestWaiters.Add(new());
        last.TrySetResult(ioEvent);
        TaskCompletionSource source = new();
        ioEventResponseWaiters.Add(source);
        return source.Task;
    }

    public FungeExecutionContextAsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
         => new(function, ioEventRequestWaiters, ioEventResponseWaiters, this, cancellationToken);

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
    public uint NextUInt32(uint exclusiveUpperBound) => random.NextUInt32(exclusiveUpperBound);

    /// <inheritdoc/>
    public float NextSingle() => random.NextSingle();

    /// <inheritdoc/>
    public void Reseed(uint seed) => random.Reseed(seed);

    /// <inheritdoc/>
    public void Reseed() => random.Reseed();

    /// <inheritdoc/>
    public void Reflect() => ip.Delta = ip.Delta.Reflect();
}

sealed class FungeCoreExecutionContext(InstructionPointer ip, FungeSpace space, FingerprintInstruction function, FungeRandomSource random)
    : FungeExecutionContext(ip, space, function, random);

sealed class FungeInputExecutionContext(InstructionPointer ip, FungeSpace space, FingerprintInstruction function, FungeRandomSource random)
    : FungeExecutionContext(ip, space, function, random), IFungeInputContext
{
    public async Task<char> ReadCharAsync()
    {
        var value = (char)0;
        void SetChar(char c) => value = c;
        await Emit(IOEvent.InputChar(SetChar));
        return value;
    }
    public async Task<int> ReadIntAsync()
    {
        var value = 0;
        void SetInt(int i) => value = i;
        await Emit(IOEvent.InputInt(SetInt));
        return value;
    }
    public async Task<string?> ReadLineAsync()
    {
        var value = (string?)null;
        void SetLine(string? line) => value = line;
        await Emit(IOEvent.InputLine(SetLine));
        return value;
    }
}

sealed class FungeOutputExecutionContext(InstructionPointer ip, FungeSpace space, FingerprintInstruction function, FungeRandomSource random)
    : FungeExecutionContext(ip, space, function, random), IFungeOutputContext
{
    public Task WriteCharAsync(char value) => Emit(IOEvent.OutputChar(value));
    public Task WriteIntAsync(int value) => Emit(IOEvent.OutputInt(value));
    public Task WriteLineAsync(string value) => Emit(IOEvent.OutputLine(value));
    public Task WriteStringAsync(string value) => Emit(IOEvent.OutputString(value));
}

sealed class FungeIoExecutionContext(InstructionPointer ip, FungeSpace space, FingerprintInstruction function, FungeRandomSource random)
    : FungeExecutionContext(ip, space, function, random), IFungeInputContext, IFungeOutputContext
{
    public async Task<char> ReadCharAsync()
    {
        var value = (char)0;
        void SetChar(char c) => value = c;
        await Emit(IOEvent.InputChar(SetChar));
        return value;
    }
    public async Task<int> ReadIntAsync()
    {
        var value = 0;
        void SetInt(int i) => value = i;
        await Emit(IOEvent.InputInt(SetInt));
        return value;
    }
    public async Task<string?> ReadLineAsync()
    {
        var value = (string?)null;
        void SetLine(string? line) => value = line;
        await Emit(IOEvent.InputLine(SetLine));
        return value;
    }
    public Task WriteCharAsync(char value) => Emit(IOEvent.OutputChar(value));
    public Task WriteIntAsync(int value) => Emit(IOEvent.OutputInt(value));
    public Task WriteLineAsync(string value) => Emit(IOEvent.OutputLine(value));
    public Task WriteStringAsync(string value) => Emit(IOEvent.OutputString(value));
}
