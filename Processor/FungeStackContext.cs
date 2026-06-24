using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor;

class FungeStackContext(FungeSpace space, int dimensions = 3) : IFungeSpaceContext
{
    public int Dimensions => dimensions;

    public int GetCell(int x, int y, int z) => space[new FungeVector(x, y, z)];
    public void SetCell(int x, int y, int z, int value) => space[new FungeVector(x, y, z)] = value;
}
