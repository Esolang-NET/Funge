namespace Esolang.Funge.Processor;

sealed partial class InstructionPointer : IFungeVectorContext
{
    /// <inheritdoc/>
    (int X, int Y, int Z) IFungeVectorContext.PopVector()
    {
        var z = StackStack.Pop();
        var y = StackStack.Pop();
        var x = StackStack.Pop();
        return (x, y, z);
    }

    /// <inheritdoc/>
    void IFungeVectorContext.PushVector(int x, int y, int z)
    {
        StackStack.Push(x);
        StackStack.Push(y);
        StackStack.Push(z);
    }
}
