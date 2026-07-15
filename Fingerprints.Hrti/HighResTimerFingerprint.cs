using System.Diagnostics;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Hrti;

/// <summary>
/// Provides the standard Funge-98 <c>HRTI</c> fingerprint (handprint <c>0x48525449</c>).
/// </summary>
public sealed class HighResTimerFingerprint(string? name = null) : IFingerprint, IFungeInstructionPointerLifecycle
{
    readonly Dictionary<int, long> _marks = [];
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"HRTI"</c>.
    /// </summary>
    public const string NAME = "HRTI";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('E', EraseMark)
            .Add('G', Granularity)
            .Add('M', Mark)
            .Add('S', SecondMicroseconds)
            .Add('T', TimeSinceMark)
            .BuildInstructions();

    static bool TryGetId(IFungeExecutionContext ctx, out int id)
    {
        if (ctx is not IFungeInstructionPointerContext ip)
        {
            id = 0;
            ctx.Reflect();
            return false;
        }

        id = ip.InstructionPointerId;
        return true;
    }

    static void Granularity(IFungeExecutionContext ctx)
    {
        // granularity in microseconds: 1_000_000 / Stopwatch.Frequency
        var gran = Stopwatch.Frequency > 0
            ? (int)(1_000_000L / Stopwatch.Frequency)
            : 1;
        ctx.Push(gran);
    }

    void Mark(IFungeExecutionContext ctx)
    {
        if (!TryGetId(ctx, out var id))
            return;
        _marks[id] = Stopwatch.GetTimestamp();
    }

    void EraseMark(IFungeExecutionContext ctx)
    {
        if (!TryGetId(ctx, out var id))
            return;
        _marks.Remove(id);
    }

    static void SecondMicroseconds(IFungeExecutionContext ctx)
    {
        var us = (int)(DateTimeOffset.UtcNow.TimeOfDay.TotalSeconds * 1_000_000 % 1_000_000);
        ctx.Push(us);
    }

    void TimeSinceMark(IFungeExecutionContext ctx)
    {
        if (!TryGetId(ctx, out var id))
            return;

        if (!_marks.TryGetValue(id, out var markTimestamp))
        {
            ctx.Reflect();
            return;
        }

        var elapsed = Stopwatch.GetTimestamp() - markTimestamp;
        var us = (int)(elapsed * 1_000_000L / Stopwatch.Frequency);
        ctx.Push(us);
    }

    /// <inheritdoc/>
    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
    {
        if (_marks.TryGetValue(parentInstructionPointerId, out var mark))
            _marks[childInstructionPointerId] = mark;
    }

    /// <inheritdoc/>
    public void OnInstructionPointerTerminated(int instructionPointerId)
        => _marks.Remove(instructionPointerId);
}
