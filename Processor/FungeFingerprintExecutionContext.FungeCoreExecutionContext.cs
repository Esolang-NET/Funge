using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
{
    sealed class FungeCoreExecutionContext(FingerprintInstruction function, IFungeExecutionContext executionContext, IFungeInstructionPointerContext instructionPointerContext, IFungeStackContext stackContext, IFungeVectorContext vectorContext, IFungeSpaceContext spaceContext, IFungeStorageOffsetContext storageOffsetContext, IFungeRandomContext randomContext)
        : FungeFingerprintExecutionContext(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext);
}
