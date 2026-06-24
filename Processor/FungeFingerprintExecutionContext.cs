using Esolang.Processor;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

/// <summary>
/// Represents the execution context for a Funge program, providing access to the instruction pointer, stack, vector, space, storage offset, and random contexts.
/// </summary>
/// <param name="Function"></param>
/// <param name="ExecutionContext"></param>
/// <param name="InstructionPointerContext"></param>
/// <param name="StackContext"></param>
/// <param name="VectorContext"></param>
/// <param name="SpaceContext"></param>
/// <param name="StorageOffsetContext"></param>
/// <param name="RandomContext"></param>
abstract partial class FungeFingerprintExecutionContext(
    FingerprintInstruction Function, 
    IFungeExecutionContext ExecutionContext, 
    IFungeInstructionPointerContext InstructionPointerContext, 
    IFungeStackContext StackContext, 
    IFungeVectorContext VectorContext, 
    IFungeSpaceContext SpaceContext, 
    IFungeStorageOffsetContext StorageOffsetContext, 
    IFungeRandomContext RandomContext
) {
    
    public static FungeFingerprintExecutionContext Create(
        FingerprintInstruction function, 
        IFungeExecutionContext executionContext, 
        IFungeInstructionPointerContext instructionPointerContext, 
        IFungeStackContext stackContext, 
        IFungeVectorContext vectorContext, 
        IFungeSpaceContext spaceContext, 
        IFungeStorageOffsetContext storageOffsetContext, 
        IFungeRandomContext randomContext, 
        bool disabledOutput = false, bool disabledInput = false
    )
        => (disabledOutput, disabledInput) switch {
        (false, false) => new FungeIoExecutionContext(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext),
        (_, false) => new FungeInputExecutionContext(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext),
        (false, _) => new FungeOutputExecutionContext(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext),
        _ => new FungeCoreExecutionContext(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext)
    };

    readonly List<TaskCompletionSource<IOEvent>> ioEventRequestWaiters = [new TaskCompletionSource<IOEvent>(TaskCreationOptions.RunContinuationsAsynchronously)];
    readonly List<TaskCompletionSource> ioEventResponseWaiters = [];

    public FungeFingerprintExecutionContextAsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
         => new(Function, ioEventRequestWaiters, ioEventResponseWaiters, this, cancellationToken);
}
