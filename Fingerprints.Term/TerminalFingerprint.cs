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

    static void ClearScreen(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        output.WriteString("\x1b[2J");
    }

    static void Home(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        output.WriteString("\x1b[H");
    }

    static void GotoPosition(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var c = ctx.Pop();
        var r = ctx.Pop();
        output.WriteString($"\x1b[{r + 1};{c + 1}H");
    }

    static void CursorDown(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var n = ctx.Pop();
        if (n > 0)
            output.WriteString($"\x1b[{n}B");
        else if (n < 0)
            output.WriteString($"\x1b[{-n}A");
    }

    static void CursorUp(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        var n = ctx.Pop();
        if (n > 0)
            output.WriteString($"\x1b[{n}A");
        else if (n < 0)
            output.WriteString($"\x1b[{-n}B");
    }

    static void ClearToEndOfLine(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        output.WriteString("\x1b[K");
    }

    static void ClearToEndOfScreen(IFungeExecutionContext ctx)
    {
        if (!TryGetOutput(ctx, out var output))
            return;
        output.WriteString("\x1b[J");
    }
}
