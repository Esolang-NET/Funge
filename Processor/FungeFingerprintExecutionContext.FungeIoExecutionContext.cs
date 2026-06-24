using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
{
    sealed class FungeIoExecutionContext : FungeFingerprintExecutionContext, IFungeInputContext, IFungeOutputContext
    {
        readonly FungeInputContext inputContext;
        readonly FungeOutputContext outputContext;
        public FungeIoExecutionContext(FingerprintInstruction function, IFungeExecutionContext executionContext, IFungeInstructionPointerContext instructionPointerContext, IFungeStackContext stackContext, IFungeVectorContext vectorContext, IFungeSpaceContext spaceContext, IFungeStorageOffsetContext storageOffsetContext, IFungeRandomContext randomContext)
            : base(function, executionContext, instructionPointerContext, stackContext, vectorContext, spaceContext, storageOffsetContext, randomContext)
        {
            inputContext = new FungeInputContext(this);
            outputContext = new FungeOutputContext(this);
        }

        public Task<char> ReadCharAsync() => inputContext.ReadCharAsync();
        
        public Task<int> ReadIntAsync() => inputContext.ReadIntAsync();

        public Task<string?> ReadLineAsync() => inputContext.ReadLineAsync();
        public Task WriteCharAsync(char value) => outputContext.WriteCharAsync(value);
        public Task WriteIntAsync(int value) => outputContext.WriteIntAsync(value);
        public Task WriteLineAsync(string value) => outputContext.WriteLineAsync(value);
        public Task WriteStringAsync(string value) => outputContext.WriteStringAsync(value);
    }
}
