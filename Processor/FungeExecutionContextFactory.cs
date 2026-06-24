using Esolang.Funge.Parser;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

/// <summary>
/// Represents a factory for creating instances of <see cref="FungeFingerprintExecutionContext"/> with the specified contexts and settings.
/// </summary>
/// <param name="ExecutionContext"></param>
/// <param name="InstructionPointerContext"></param>
/// <param name="StackContext"></param>
/// <param name="VectorContext"></param>
/// <param name="SpaceContext"></param>
/// <param name="StorageOffsetContext"></param>
/// <param name="RandomContext"></param>
/// <param name="DisabledOutput"></param>
/// <param name="DisabledInput"></param>
record FungeExecutionContextFactory(IFungeExecutionContext ExecutionContext, IFungeInstructionPointerContext InstructionPointerContext, IFungeStackContext StackContext, IFungeVectorContext VectorContext, IFungeSpaceContext SpaceContext, IFungeStorageOffsetContext StorageOffsetContext, IFungeRandomContext RandomContext, bool DisabledOutput = false, bool DisabledInput = false)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FungeExecutionContextFactory"/> class with the specified instruction pointer, space, random source, and optional output/input settings.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="space"></param>
    /// <param name="random"></param>
    /// <param name="disabledOutput"></param>
    /// <param name="disabledInput"></param>
    public FungeExecutionContextFactory(InstructionPointer ip, FungeSpace space, FungeRandomSource random, bool disabledOutput = false, bool disabledInput = false) : this(ip, ip, ip, ip, new FungeStackContext(space), ip, random, disabledOutput, disabledInput) { }

    /// <summary>
    /// Creates a new instance of <see cref="FungeFingerprintExecutionContext"/> with the specified function and the contexts/settings provided to this factory.
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public FungeFingerprintExecutionContext CreateContext(FingerprintInstruction function) => FungeFingerprintExecutionContext.Create(function, ExecutionContext, InstructionPointerContext, StackContext, VectorContext, SpaceContext, StorageOffsetContext, RandomContext, DisabledOutput, DisabledInput);
}
