using Esolang.Processor;
using System.Runtime.CompilerServices;

namespace Esolang.Funge.Processor;

class FungeOutputContext(IFungeEventEmitter emitEvent) : IFungeOutputContext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task WriteCharAsync(char value) => emitEvent.Emit(IOEvent.OutputChar(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task WriteIntAsync(int value) => emitEvent.Emit(IOEvent.OutputInt(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task WriteLineAsync(string value) => emitEvent.Emit(IOEvent.OutputLine(value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task WriteStringAsync(string value) => emitEvent.Emit(IOEvent.OutputString(value));
}
