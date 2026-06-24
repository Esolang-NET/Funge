using Esolang.Funge.Parser;
using System.Runtime.CompilerServices;

namespace Esolang.Funge.Processor;

sealed class FungeStackContext(FungeSpace space, int dimensions = 3) : IFungeSpaceContext
{
    public int Dimensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => dimensions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCell(int x, int y, int z) => space[new FungeVector(x, y, z)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCell(int x, int y, int z, int value) => space[new FungeVector(x, y, z)] = value;
}
