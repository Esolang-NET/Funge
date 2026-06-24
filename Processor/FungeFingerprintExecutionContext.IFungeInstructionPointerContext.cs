namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeInstructionPointerContext
{
    /// <inheritdoc/>
    public int InstructionPointerId => InstructionPointerContext.InstructionPointerId;
}
