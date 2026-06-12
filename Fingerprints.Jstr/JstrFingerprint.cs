namespace Esolang.Funge.Fingerprints.Jstr;

/// <summary>
/// Provides the standard Funge-98 <c>JSTR</c> fingerprint (handprint <c>0x4A535452</c>).
/// </summary>
public sealed class JstrFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("JSTR");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="JstrFingerprint"/>.</summary>
    public JstrFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('G', GetString)
            .Add('P', PutString)
            .BuildInstructions();

    static bool TryGetContexts(
        IFungeExecutionContext ctx,
        out IFungeVectorContext vector,
        out IFungeSpaceContext space)
    {
        if (ctx is not IFungeVectorContext foundVector || ctx is not IFungeSpaceContext foundSpace)
        {
            vector = null!;
            space = null!;
            ctx.Reflect();
            return false;
        }

        vector = foundVector;
        space = foundSpace;
        return true;
    }

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var chars = new System.Collections.Generic.List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        return new string([.. chars]);
    }

    static void Push0gnirts(IFungeExecutionContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    static void GetString(IFungeExecutionContext ctx)
    {
        if (!TryGetContexts(ctx, out var vector, out var space))
            return;

        var n = ctx.Pop();
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        var pos = vector.PopVector();
        var delta = vector.PopVector();

        var chars = new char[n];
        var (x, y, z) = pos;
        var (dx, dy, dz) = delta;
        for (var i = 0; i < n; i++)
        {
            chars[i] = (char)space.GetCell(x, y, z);
            x += dx;
            y += dy;
            z += dz;
        }

        Push0gnirts(ctx, new string(chars));
    }

    static void PutString(IFungeExecutionContext ctx)
    {
        if (!TryGetContexts(ctx, out var vector, out var space))
            return;

        var n = ctx.Pop();
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        var pos = vector.PopVector();
        var delta = vector.PopVector();
        var str = Pop0gnirts(ctx);

        var count = Math.Min(n, str.Length);
        var (x, y, z) = pos;
        var (dx, dy, dz) = delta;
        for (var i = 0; i < count; i++)
        {
            space.SetCell(x, y, z, str[i]);
            x += dx;
            y += dy;
            z += dz;
        }
    }
}
