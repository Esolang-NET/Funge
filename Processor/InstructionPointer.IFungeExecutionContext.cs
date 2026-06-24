namespace Esolang.Funge.Processor;

sealed partial class InstructionPointer : IFungeExecutionContext
{

    void IFungeExecutionContext.Push(int value) => StackStack.Push(value);
    int IFungeExecutionContext.Pop() => StackStack.Pop();
    int IFungeExecutionContext.Peek() => StackStack.Peek();
    void IFungeExecutionContext.Reflect() => Delta = Delta.Reflect();
}
