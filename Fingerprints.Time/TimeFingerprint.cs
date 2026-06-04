namespace Esolang.Funge;

/// <summary>
/// Provides the standard Funge-98 <c>TIME</c> fingerprint (handprint <c>0x54494D45</c>).
/// </summary>
public sealed class TimeFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("TIME");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="TimeFingerprint"/>.</summary>
    public TimeFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('D', GetDate)
            .Add('G', GetGmt)
            .Add('H', static ctx => ctx.Push(System.DateTime.Now.Hour))
            .Add('M', static ctx => ctx.Push(System.DateTime.Now.Minute))
            .Add('S', static ctx => ctx.Push(System.DateTime.Now.Second))
            .Add('T', GetTime)
            .Add('Y', static ctx => ctx.Push(System.DateTime.Now.Year))
            .BuildInstructions();

    static void GetDate(IFungeExecutionContext ctx)
    {
        var now = System.DateTime.Now;
        ctx.Push(now.Day);
        ctx.Push(now.Month);
        ctx.Push(now.Year);
    }

    static void GetGmt(IFungeExecutionContext ctx)
    {
        var now = System.DateTime.UtcNow;
        ctx.Push(now.Second);
        ctx.Push(now.Minute);
        ctx.Push(now.Hour);
        ctx.Push(now.Day);
        ctx.Push(now.Month);
        ctx.Push(now.Year);
    }

    static void GetTime(IFungeExecutionContext ctx)
    {
        var now = System.DateTime.Now;
        ctx.Push(now.Second);
        ctx.Push(now.Minute);
        ctx.Push(now.Hour);
    }
}
