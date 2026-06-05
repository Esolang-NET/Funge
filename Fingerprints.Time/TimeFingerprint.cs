namespace Esolang.Funge.Fingerprints.Time;

/// <summary>
/// Provides the standard Funge-98 <c>TIME</c> fingerprint (handprint <c>0x54494D45</c>).
/// </summary>
public sealed class TimeFingerprint : IFingerprint
{
    bool _useGmt;

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

    DateTime CurrentTime => _useGmt ? DateTime.UtcNow : DateTime.Now;

    void GetDayOfMonth(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Day);

    void GetDayOfYear(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.DayOfYear - 1);

    void UseGmt(IFungeExecutionContext ctx) => _useGmt = true;

    void GetHour(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Hour);

    void UseLocalTime(IFungeExecutionContext ctx) => _useGmt = false;

    void GetMinute(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Minute);

    void GetMonth(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Month);

    void GetSecond(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Second);

    void GetDayOfWeek(IFungeExecutionContext ctx) => ctx.Push((int)CurrentTime.DayOfWeek + 1);

    void GetYear(IFungeExecutionContext ctx) => ctx.Push(CurrentTime.Year);
}
