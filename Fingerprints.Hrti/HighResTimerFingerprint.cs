using System.Diagnostics;

namespace Esolang.Funge.Fingerprints.Hrti;

/// <summary>
/// Provides the standard Funge-98 <c>HRTI</c> fingerprint (handprint <c>0x48525449</c>).
/// </summary>
public sealed class HighResTimerFingerprint : IFingerprint, IFungeInstructionPointerLifecycle
{
    readonly Dictionary<int, long> _marks = [];

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("HRTI");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="HighResTimerFingerprint"/>.</summary>
    public HighResTimerFingerprint() =>
        Instructions = new FingerprintBuilder()
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
