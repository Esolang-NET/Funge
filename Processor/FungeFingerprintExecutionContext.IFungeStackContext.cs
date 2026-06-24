namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
    : IFungeStackContext
{
    /// <inheritdoc/>
    public int StackDepth => StackContext.StackDepth;
}
