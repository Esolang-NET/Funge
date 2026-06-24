namespace Esolang.Funge.Processor;

sealed partial class InstructionPointer : IFungeStackContext
{
    int IFungeStackContext.StackDepth => StackStack.TOSS.Count;
}
