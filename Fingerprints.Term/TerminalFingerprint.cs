using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Term;

/// <summary>
/// Provides the standard Funge-98 <c>TERM</c> fingerprint (handprint <c>0x5445524D</c>).
/// </summary>
public sealed class TerminalFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("TERM");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="TerminalFingerprint"/>.</summary>
    public TerminalFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('C', ClearScreen)
            .Add('D', CursorDown)
            .Add('G', GotoPosition)
            .Add('H', Home)
            .Add('L', ClearToEndOfLine)
            .Add('S', ClearToEndOfScreen)
            .Add('U', CursorUp)
            .BuildInstructions();

    static bool TryGetOutput(IFungeExecutionContext ctx, out IFungeOutputContext output)
    {
        if (ctx is not IFungeOutputContext found)
        {
            output = null!;
            ctx.Reflect();
            return false;
        }

        output = found;
        return true;
    }

    static async ValueTask ClearScreen(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        await output.WriteStringAsync("\x1b[2J");
    }

    static async ValueTask Home(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        await output.WriteStringAsync("\x1b[H");
    }

    static async ValueTask GotoPosition(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var c = ctx.Pop();
        var r = ctx.Pop();
        await output.WriteStringAsync($"\x1b[{r + 1};{c + 1}H");
    }

    static async ValueTask CursorDown(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var n = ctx.Pop();
        if (n > 0)
            await output.WriteStringAsync($"\x1b[{n}B");
        else if (n < 0)
            await output.WriteStringAsync($"\x1b[{-n}A");
    }

    static async ValueTask CursorUp(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var n = ctx.Pop();
        if (n > 0)
            await output.WriteStringAsync($"\x1b[{n}A");
        else if (n < 0)
            await output.WriteStringAsync($"\x1b[{-n}B");
    }

    static async ValueTask ClearToEndOfLine(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        await output.WriteStringAsync("\x1b[K");
    }

    static async ValueTask ClearToEndOfScreen(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        await output.WriteStringAsync("\x1b[J");
    }
}
