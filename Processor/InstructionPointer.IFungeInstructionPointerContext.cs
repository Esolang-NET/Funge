namespace Esolang.Funge.Processor;

sealed partial class InstructionPointer : IFungeInstructionPointerContext
{
    int IFungeInstructionPointerContext.InstructionPointerId => Id;
}
