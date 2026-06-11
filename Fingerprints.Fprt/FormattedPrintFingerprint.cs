using System.Globalization;
using System.Text.RegularExpressions;

namespace Esolang.Funge.Fingerprints.Fprt;

/// <summary>
/// Provides the standard Funge-98 <c>FPRT</c> fingerprint (handprint <c>0x46505254</c>).
/// </summary>
public sealed partial class FormattedPrintFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("FPRT");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="FormattedPrintFingerprint"/>.</summary>
    public FormattedPrintFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('D', FormatDouble)
            .Add('F', FormatFloat)
            .Add('I', FormatInteger)
            .Add('L', FormatLong)
            .Add('S', FormatString)
            .BuildInstructions();

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"%[^%]*[diouxXeEfgGsq]")]
    private static partial Regex FormatSpecifierRegex();
#else
    private static Regex FormatSpecifierRegex() => new Regex(@"%[^%]*[diouxXeEfgGsq]");
#endif

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var chars = new List<char>();
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

    static bool TryGetSingleSpecifier(string fmt, out Match match)
    {
        var matches = FormatSpecifierRegex().Matches(fmt);
        if (matches.Count != 1)
        {
            match = default!;
            return false;
        }

        match = matches[0];
        return true;
    }

    static string FormatInteger(string fmt, int value)
    {
        if (!TryGetSingleSpecifier(fmt, out var m))
            return string.Empty;

        var spec = m.Value;
        var lastChar = spec[^1];
        var innerFmt = spec[1..^1]; // e.g. "05", ".3", etc.

        var result = lastChar switch
        {
            'd' or 'i' => ParsedFormat(innerFmt, value),
            'o' => Convert.ToString(value, 8),
            'u' => unchecked((uint)value).ToString(CultureInfo.InvariantCulture),
            'x' => value.ToString("x", CultureInfo.InvariantCulture),
            'X' => value.ToString("X", CultureInfo.InvariantCulture),
            _ => value.ToString(CultureInfo.InvariantCulture),
        };

        return fmt.Replace(spec, result);
    }

    static string ParsedFormat(string innerFmt, int value)
    {
        // Parse width and sign from innerFmt like "5", "-10", "+5", "05"
        if (string.IsNullOrEmpty(innerFmt))
            return value.ToString(CultureInfo.InvariantCulture);

        var zeroPad = innerFmt.Contains('0');
        var leftAlign = innerFmt.Contains('-');
        var widthStr = innerFmt.TrimStart('+', '-', '0', ' ');
        if (!int.TryParse(widthStr, out var width))
            return value.ToString(CultureInfo.InvariantCulture);

        var s = value.ToString(CultureInfo.InvariantCulture);
        if (zeroPad && width > s.Length)
            s = s.PadLeft(width, '0');
        else if (leftAlign)
            s = s.PadRight(width);
        else if (width > 0)
            s = s.PadLeft(width);
        return s;
    }

    static string FormatFloat(string fmt, float value)
    {
        if (!TryGetSingleSpecifier(fmt, out var m))
            return string.Empty;

        var spec = m.Value;
        var lastChar = spec[^1];
        var innerFmt = spec[1..^1];

        var precision = 6;
        if (innerFmt.Contains('.'))
        {
            var dotIdx = innerFmt.IndexOf('.');
            if (int.TryParse(innerFmt[(dotIdx + 1)..], out var p))
                precision = p;
        }

        var result = lastChar switch
        {
            'e' => value.ToString($"e{precision}", CultureInfo.InvariantCulture),
            'E' => value.ToString($"E{precision}", CultureInfo.InvariantCulture),
            'g' or 'G' => value.ToString($"G{precision}", CultureInfo.InvariantCulture),
            _ => value.ToString($"F{precision}", CultureInfo.InvariantCulture),
        };
        return fmt.Replace(spec, result);
    }

    static string FormatDouble(string fmt, double value)
    {
        if (!TryGetSingleSpecifier(fmt, out var m))
            return string.Empty;

        var spec = m.Value;
        var lastChar = spec[^1];
        var innerFmt = spec[1..^1];

        var precision = 6;
        if (innerFmt.Contains('.'))
        {
            var dotIdx = innerFmt.IndexOf('.');
            if (int.TryParse(innerFmt[(dotIdx + 1)..], out var p))
                precision = p;
        }

        var result = lastChar switch
        {
            'e' => value.ToString($"e{precision}", CultureInfo.InvariantCulture),
            'E' => value.ToString($"E{precision}", CultureInfo.InvariantCulture),
            'g' or 'G' => value.ToString($"G{precision}", CultureInfo.InvariantCulture),
            _ => value.ToString($"F{precision}", CultureInfo.InvariantCulture),
        };
        return fmt.Replace(spec, result);
    }

    static string FormatLong(string fmt, long value)
    {
        if (!TryGetSingleSpecifier(fmt, out var m))
            return string.Empty;

        var spec = m.Value;
        return fmt.Replace(spec, value.ToString(CultureInfo.InvariantCulture));
    }

    static string FormatStringValue(string fmt, string value)
    {
        if (!TryGetSingleSpecifier(fmt, out var m))
            return string.Empty;

        var spec = m.Value;
        return fmt.Replace(spec, value);
    }

    static void FormatInteger(IFungeExecutionContext ctx)
    {
        var fmt = Pop0gnirts(ctx);
        var value = ctx.Pop();
        if (!TryGetSingleSpecifier(fmt, out _))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, FormatInteger(fmt, value));
    }

    static void FormatFloat(IFungeExecutionContext ctx)
    {
        var fmt = Pop0gnirts(ctx);
        var value = Int32ToSingle(ctx.Pop());
        if (!TryGetSingleSpecifier(fmt, out _))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, FormatFloat(fmt, value));
    }

    static float Int32ToSingle(int bits) =>
#if NETSTANDARD2_0
        BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
#else
        BitConverter.Int32BitsToSingle(bits);
#endif

    static void FormatDouble(IFungeExecutionContext ctx)
    {
        var fmt = Pop0gnirts(ctx);
        // FPDP: pop upper then lower
        var upper = ctx.Pop();
        var lower = ctx.Pop();
        var bits = ((long)upper << 32) | unchecked((uint)lower);
        var value = BitConverter.Int64BitsToDouble(bits);
        if (!TryGetSingleSpecifier(fmt, out _))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, FormatDouble(fmt, value));
    }

    static void FormatLong(IFungeExecutionContext ctx)
    {
        var fmt = Pop0gnirts(ctx);
        // LONG: pop high then low
        var high = ctx.Pop();
        var low = ctx.Pop();
        var value = ((long)high << 32) | unchecked((uint)low);
        if (!TryGetSingleSpecifier(fmt, out _))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, FormatLong(fmt, value));
    }

    static void FormatString(IFungeExecutionContext ctx)
    {
        var fmt = Pop0gnirts(ctx);
        var value = Pop0gnirts(ctx);
        if (!TryGetSingleSpecifier(fmt, out _))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, FormatStringValue(fmt, value));
    }
}
