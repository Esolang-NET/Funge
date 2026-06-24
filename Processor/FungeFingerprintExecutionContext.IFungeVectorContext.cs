namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeVectorContext
{

    /// <inheritdoc/>
    public (int X, int Y, int Z) PopVector() => VectorContext.PopVector();

    /// <inheritdoc/>
    public void PushVector(int x, int y, int z) => VectorContext.PushVector(x, y, z);

}
