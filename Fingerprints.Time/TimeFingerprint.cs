namespace Esolang.Funge.Fingerprints.Time;

/// <summary>
/// Provides the standard Funge-98 <c>TIME</c> fingerprint (handprint <c>0x54494D45</c>).
/// </summary>
public sealed class TimeFingerprint : IFingerprint, IFungeInstructionPointerLifecycle
{
    readonly Dictionary<int, bool> _useGmtByInstructionPointer = [];

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("TIME");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="TimeFingerprint"/>.</summary>
    public TimeFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('D', GetDayOfMonth)
            .Add('F', GetDayOfYear)
            .Add('G', UseGmt)
            .Add('H', GetHour)
            .Add('L', UseLocalTime)
            .Add('M', GetMinute)
            .Add('O', GetMonth)
            .Add('S', GetSecond)
            .Add('W', GetDayOfWeek)
            .Add('Y', GetYear)
            .BuildInstructions();

    DateTime CurrentTime(IFungeExecutionContext ctx)
        => GetUseGmt(ctx) ? DateTime.UtcNow : DateTime.Now;

    void GetDayOfMonth(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Day);

    void GetDayOfYear(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).DayOfYear - 1);

    void UseGmt(IFungeExecutionContext ctx)
    {
        if (!TryGetInstructionPointerId(ctx, out var instructionPointerId))
            return;

        _useGmtByInstructionPointer[instructionPointerId] = true;
    }

    void GetHour(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Hour);

    void UseLocalTime(IFungeExecutionContext ctx)
    {
        if (!TryGetInstructionPointerId(ctx, out var instructionPointerId))
            return;

        _useGmtByInstructionPointer[instructionPointerId] = false;
    }

    void GetMinute(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Minute);

    void GetMonth(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Month);

    void GetSecond(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Second);

    void GetDayOfWeek(IFungeExecutionContext ctx) => ctx.Push((int)CurrentTime(ctx).DayOfWeek + 1);

    void GetYear(IFungeExecutionContext ctx) => ctx.Push(CurrentTime(ctx).Year);

    bool GetUseGmt(IFungeExecutionContext ctx)
        => TryGetInstructionPointerId(ctx, out var instructionPointerId)
            && _useGmtByInstructionPointer.TryGetValue(instructionPointerId, out var useGmt)
            && useGmt;

    static bool TryGetInstructionPointerId(IFungeExecutionContext ctx, out int instructionPointerId)
    {
        if (ctx is not IFungeInstructionPointerContext instructionPointer)
        {
            instructionPointerId = 0;
            ctx.Reflect();
            return false;
        }

        instructionPointerId = instructionPointer.InstructionPointerId;
        return true;
    }

    /// <inheritdoc/>
    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
    {
        if (_useGmtByInstructionPointer.TryGetValue(parentInstructionPointerId, out var useGmt))
            _useGmtByInstructionPointer[childInstructionPointerId] = useGmt;
    }

    /// <inheritdoc/>
    public void OnInstructionPointerTerminated(int instructionPointerId)
        => _useGmtByInstructionPointer.Remove(instructionPointerId);
}
