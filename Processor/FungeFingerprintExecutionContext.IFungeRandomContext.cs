namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeRandomContext
{
    /// <inheritdoc/>
    public uint NextUInt32(uint exclusiveUpperBound) => RandomContext.NextUInt32(exclusiveUpperBound);

    /// <inheritdoc/>
    public float NextSingle() => RandomContext.NextSingle();

    /// <inheritdoc/>
    public void Reseed(uint seed) => RandomContext.Reseed(seed);

    /// <inheritdoc/>
    public void Reseed() => RandomContext.Reseed();
}
