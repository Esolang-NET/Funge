using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
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
            .Add('D', Display)
            .Add('F', Search)
            .Add('G', Get)
            .Add('I', Input)
            .Add('L', Left)
            .Add('M', Slice)
            .Add('N', Length)
            .Add('P', Put)
            .Add('R', Right)
            .Add('S', NumberToString)
            .Add('V', StringToNumber)
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
        var upper = Pop0gnirts(ctx);
        var lower = Pop0gnirts(ctx);
        Push0gnirts(ctx, upper + lower);
    }

    static void Compare(IFungeExecutionContext ctx)
    {
        var upper = Pop0gnirts(ctx);
        var lower = Pop0gnirts(ctx);
        ctx.Push(CompareStrings(upper, lower));
    }

    static int CompareStrings(string upper, string lower)
    {
        var max = Math.Max(upper.Length, lower.Length);
        for (var i = 0; i <= max; i++)
        {
            var upperChar = i < upper.Length ? upper[i] : '\0';
            var lowerChar = i < lower.Length ? lower[i] : '\0';
            var diff = upperChar - lowerChar;
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    static async ValueTask Display(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        await output.WriteStringAsync(Pop0gnirts(ctx));
    }

    static void Search(IFungeExecutionContext ctx)
    {
        var upper = Pop0gnirts(ctx);
        var lower = Pop0gnirts(ctx);
        var index = upper.IndexOf(lower, StringComparison.Ordinal);
        Push0gnirts(ctx, index >= 0 ? upper[index..] : string.Empty);
    }

    static bool TryGetOffsetSpaceVectorContexts(
        IFungeExecutionContext ctx,
        out IFungeVectorContext vector,
        out IFungeSpaceContext space,
        out IFungeStorageOffsetContext offset)
    {
        if (ctx is not IFungeVectorContext foundVector
            || ctx is not IFungeSpaceContext foundSpace
            || ctx is not IFungeStorageOffsetContext foundOffset)
        {
            vector = null!;
            space = null!;
            offset = null!;
            ctx.Reflect();
            return false;
        }

        vector = foundVector;
        space = foundSpace;
        offset = foundOffset;
        return true;
    }

    static void Get(IFungeExecutionContext ctx)
    {
        if (!TryGetOffsetSpaceVectorContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (x, y, z) = vector.PopVector();
        var (offsetX, offsetY, offsetZ) = offset.StorageOffset;
        List<char> chars = [];
        while (true)
        {
            var value = space.GetCell(x + offsetX, y + offsetY, z + offsetZ);
            if (value == 0)
                break;

            chars.Add((char)value);
            x++;
        }

        Push0gnirts(ctx, new string([.. chars]));
    }

    static async ValueTask Input(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeInputContext input)
        {
            ctx.Reflect();
            return;
        }

        var line = await input.ReadLineAsync();
        if (line is null)
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, line);
    }

    static void Left(IFungeExecutionContext ctx)
    {
        var n = ctx.Pop();
        var s = Pop0gnirts(ctx);
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, s[..Math.Min(n, s.Length)]);
    }

    static void Slice(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var start = ctx.Pop();
        var s = Pop0gnirts(ctx);
        if (start < 0 || count < 0 || start > s.Length)
        {
            ctx.Reflect();
            return;
        }

        var actualCount = Math.Min(count, s.Length - start);
        Push0gnirts(ctx, s.Substring(start, actualCount));
    }

    static void Length(IFungeExecutionContext ctx)
    {
        var s = Pop0gnirts(ctx);
        Push0gnirts(ctx, s);
        ctx.Push(s.Length);
    }

    static void Put(IFungeExecutionContext ctx)
    {
        if (!TryGetOffsetSpaceVectorContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (x, y, z) = vector.PopVector();
        var (offsetX, offsetY, offsetZ) = offset.StorageOffset;
        var s = Pop0gnirts(ctx);
        for (var i = 0; i < s.Length; i++)
            space.SetCell(x + offsetX + i, y + offsetY, z + offsetZ, s[i]);
        space.SetCell(x + offsetX + s.Length, y + offsetY, z + offsetZ, 0);
    }

    static void Right(IFungeExecutionContext ctx)
    {
        var n = ctx.Pop();
        var s = Pop0gnirts(ctx);
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        var count = Math.Min(n, s.Length);
        Push0gnirts(ctx, s[^count..]);
    }

    static void NumberToString(IFungeExecutionContext ctx)
    {
        var n = ctx.Pop();
        Push0gnirts(ctx, n.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    static void StringToNumber(IFungeExecutionContext ctx)
    {
        var s = Pop0gnirts(ctx);
        ctx.Push(ParseAtoiLike(s));
    }

    static int ParseAtoiLike(string s)
    {
        var i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i]))
            i++;

        var sign = 1;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
        {
            sign = s[i] == '-' ? -1 : 1;
            i++;
        }

        long value = 0;
        var foundDigit = false;
        while (i < s.Length && char.IsDigit(s[i]))
        {
            foundDigit = true;
            var digit = s[i] - '0';
            if (value > (long.MaxValue - digit) / 10)
                return sign > 0 ? int.MaxValue : int.MinValue;
            value = value * 10 + digit;
            if (sign > 0 && value >= int.MaxValue)
                return int.MaxValue;
            if (sign < 0 && -value <= int.MinValue)
                return int.MinValue;
            i++;
        }

        if (!foundDigit)
            return 0;

        value *= sign;
        if (value > int.MaxValue)
            return int.MaxValue;
        if (value < int.MinValue)
            return int.MinValue;
        return (int)value;
    }
}
