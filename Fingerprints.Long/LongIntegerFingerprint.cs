using System.Globalization;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Long;

/// <summary>
/// Provides the standard Funge-98 <c>LONG</c> fingerprint (handprint <c>0x4C4F4E47</c>).
/// </summary>
public sealed class LongIntegerFingerprint : IFingerprint
{
    const int LongBitWidth = sizeof(long) * 8;

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("LONG");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="LongIntegerFingerprint"/>.</summary>
    public LongIntegerFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', Add)
            .Add('B', AbsoluteValue)
            .Add('D', Divide)
            .Add('E', SignExtend)
            .Add('L', ShiftLeft)
            .Add('M', Multiply)
            .Add('N', Negate)
            .Add('O', Modulo)
            .Add('P', Print)
            .Add('R', ShiftRight)
            .Add('S', Subtract)
            .Add('Z', AsciiToLong)
            .BuildInstructions();

    static long PopLong(IFungeExecutionContext ctx)
    {
        var low = unchecked((uint)ctx.Pop());
        var high = ctx.Pop();
        return ((long)high << 32) | low;
    }

    static void PushLong(IFungeExecutionContext ctx, long value)
    {
        ctx.Push(unchecked((int)(value >> 32)));
        ctx.Push(unchecked((int)value));
    }

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var chars = new List<char>();
        int value;
        while ((value = ctx.Pop()) != 0)
            chars.Insert(0, (char)value);

        return new string([.. chars]);
    }

    static int NormalizeShiftCount(int count)
        => (int)(Math.Abs((long)count) % LongBitWidth);

    static void Add(IFungeExecutionContext ctx)
    {
        var right = PopLong(ctx);
        var left = PopLong(ctx);
        PushLong(ctx, unchecked(left + right));
    }

    static void AbsoluteValue(IFungeExecutionContext ctx)
    {
        var value = PopLong(ctx);
        PushLong(ctx, value == long.MinValue ? value : Math.Abs(value));
    }

    static void Divide(IFungeExecutionContext ctx)
    {
        var divisor = PopLong(ctx);
        var dividend = PopLong(ctx);
        PushLong(ctx, divisor == 0 ? 0 : dividend / divisor);
    }

    static void SignExtend(IFungeExecutionContext ctx)
        => PushLong(ctx, ctx.Pop());

    static void ShiftLeft(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var value = PopLong(ctx);
        var shift = NormalizeShiftCount(count);
        PushLong(ctx, count < 0 ? value >> shift : value << shift);
    }

    static void Multiply(IFungeExecutionContext ctx)
    {
        var right = PopLong(ctx);
        var left = PopLong(ctx);
        PushLong(ctx, unchecked(left * right));
    }

    static void Negate(IFungeExecutionContext ctx)
        => PushLong(ctx, unchecked(-PopLong(ctx)));

    static void Modulo(IFungeExecutionContext ctx)
    {
        var divisor = PopLong(ctx);
        var dividend = PopLong(ctx);
        PushLong(ctx, divisor == 0 ? 0 : dividend % divisor);
    }

    static void Print(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        output.WriteString(PopLong(ctx).ToString(CultureInfo.InvariantCulture));
        output.WriteString(" ");
    }

    static void ShiftRight(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var value = PopLong(ctx);
        var shift = NormalizeShiftCount(count);
        PushLong(ctx, count < 0 ? value << shift : value >> shift);
    }

    static void Subtract(IFungeExecutionContext ctx)
    {
        var right = PopLong(ctx);
        var left = PopLong(ctx);
        PushLong(ctx, unchecked(left - right));
    }

    static void AsciiToLong(IFungeExecutionContext ctx)
    {
        var value = Pop0gnirts(ctx);
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            ctx.Reflect();
            return;
        }

        PushLong(ctx, result);
    }
}
