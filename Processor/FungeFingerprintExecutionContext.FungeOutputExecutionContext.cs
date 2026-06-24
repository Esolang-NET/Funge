using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
{
    sealed class FungeOutputExecutionContext : FungeFingerprintExecutionContext, IFungeOutputContext
    {
        readonly FungeOutputContext outputContext;
        public FungeOutputExecutionContext(FingerprintInstruction function, IFungeExecutionContext executionContext, IFungeInstructionPointerContext instructionPointerContext, IFungeStackContext stackContext, IFungeVectorContext vectorContext, IFungeSpaceContext spaceContext, IFungeStorageOffsetContext storageOffsetContext, IFungeRandomContext randomContext)
            : base(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext)
            => outputContext = new FungeOutputContext(this);
        
        public Task WriteCharAsync(char value) => outputContext.WriteCharAsync(value);
        public Task WriteIntAsync(int value) => outputContext.WriteIntAsync(value);
        public Task WriteLineAsync(string value) => outputContext.WriteLineAsync(value);
        public Task WriteStringAsync(string value) => outputContext.WriteStringAsync(value);
    }
}
