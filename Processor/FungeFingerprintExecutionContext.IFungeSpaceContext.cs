namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext : IFungeSpaceContext
{
    
    /// <inheritdoc/>
    public int Dimensions => SpaceContext.Dimensions;

    /// <inheritdoc/>
    public int GetCell(int x, int y, int z) => SpaceContext.GetCell(x, y, z);

    /// <inheritdoc/>
    public void SetCell(int x, int y, int z, int value) => SpaceContext.SetCell(x, y, z, value);

}
