namespace Esolang.Funge.Fingerprints.Strn;

/// <summary>
/// Provides the standard Funge-98 <c>STRN</c> fingerprint (handprint <c>0x5354524E</c>).
/// </summary>
public sealed class StringFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("STRN");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="StringFingerprint"/>.</summary>
    public StringFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', Append)
            .Add('C', Compare)
            .Add('L', Length)
            .Add('N', NumberToString)
            .Add('R', Reverse)
            .Add('S', StringToNumber)
            .BuildInstructions();

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        int c;
        while ((c = ctx.Pop()) != 0)
            sb.Insert(0, (char)c);
        return sb.ToString();
    }

    static void Push0gnirts(IFungeExecutionContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    static void Append(IFungeExecutionContext ctx)
    {
        var s1 = Pop0gnirts(ctx);
        var s2 = Pop0gnirts(ctx);
        Push0gnirts(ctx, s2 + s1);
    }

    static void Compare(IFungeExecutionContext ctx)
    {
        var s1 = Pop0gnirts(ctx);
        var s2 = Pop0gnirts(ctx);

        var res = string.CompareOrdinal(s1, s2);
        ctx.Push(res > 0 ? 1 : (res < 0 ? -1 : 0));
    }

    static void Length(IFungeExecutionContext ctx)
    {
        var s = Pop0gnirts(ctx);
        ctx.Push(s.Length);
    }

    static void NumberToString(IFungeExecutionContext ctx)
    {
        var n = ctx.Pop();
        Push0gnirts(ctx, n.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    static void Reverse(IFungeExecutionContext ctx)
    {
        var s = Pop0gnirts(ctx);
        var chars = s.ToCharArray();

        Array.Reverse(chars);
        Push0gnirts(ctx, new string(chars));
    }

    static void StringToNumber(IFungeExecutionContext ctx)
    {
        var s = Pop0gnirts(ctx);
        if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n))
        {
            ctx.Push(n);
        }
        else
        {
            ctx.Reflect();
        }
    }
}
