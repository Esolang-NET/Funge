namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeStorageOffsetContext
{
    /// <inheritdoc/>
    public (int X, int Y, int Z) StorageOffset => StorageOffsetContext.StorageOffset;
}
