using Esolang.Processor;
using System.Runtime.CompilerServices;

namespace Esolang.Funge.Processor;

class FungeInputContext(IFungeEventEmitter emitEvent) : IFungeInputContext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<char> ReadCharAsync()
    {
        var value = (char)0;
        void SetChar(char c) => value = c;
        await emitEvent.Emit(IOEvent.InputChar(SetChar));
        return value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<int> ReadIntAsync()
    {
        var value = 0;
        void SetInt(int i) => value = i;
        await emitEvent.Emit(IOEvent.InputInt(SetInt));
        return value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<string?> ReadLineAsync()
    {
        var value = (string?)null;
        void SetLine(string? line) => value = line;
        await emitEvent.Emit(IOEvent.InputLine(SetLine));
        return value;
    }
}
