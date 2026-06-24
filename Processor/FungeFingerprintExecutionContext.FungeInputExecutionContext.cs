using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
{
    sealed class FungeInputExecutionContext : FungeFingerprintExecutionContext, IFungeInputContext
    {
        readonly FungeInputContext inputContext;
        public FungeInputExecutionContext(FingerprintInstruction function, IFungeExecutionContext executionContext, IFungeInstructionPointerContext instructionPointerContext, IFungeStackContext stackContext, IFungeVectorContext vectorContext, IFungeSpaceContext spaceContext, IFungeStorageOffsetContext storageOffsetContext, IFungeRandomContext randomContext)
            : base(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext)
           => inputContext = new FungeInputContext(this);
        public Task<char> ReadCharAsync() => inputContext.ReadCharAsync();
        
        public Task<int> ReadIntAsync() => inputContext.ReadIntAsync();

        public Task<string?> ReadLineAsync() => inputContext.ReadLineAsync();
    }
}
