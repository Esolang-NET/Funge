namespace Esolang.Funge.Processor;

/// <summary>
/// Represents the execution state of a single Instruction Pointer (IP) in Funge-98.
/// </summary>
sealed partial class InstructionPointer : IFungeStorageOffsetContext
{
    (int X, int Y, int Z) IFungeStorageOffsetContext.StorageOffset => (Offset.X, Offset.Y, Offset.Z);
}
