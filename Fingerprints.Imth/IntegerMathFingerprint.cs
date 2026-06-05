using System.Globalization;

namespace Esolang.Funge.Fingerprints.Imth;

/// <summary>
/// Provides the standard Funge-98 <c>IMTH</c> fingerprint (handprint <c>0x494D5448</c>).
/// </summary>
public sealed class IntegerMathFingerprint : IFingerprint
{
    const int CellBitWidth = sizeof(int) * 8;

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("IMTH");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="IntegerMathFingerprint"/>.</summary>
    public IntegerMathFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', Average)
            .Add('B', AbsoluteValue)
            .Add('C', MultiplyByHundred)
            .Add('D', DecrementTowardsZero)
            .Add('E', MultiplyByTenThousand)
            .Add('F', Factorial)
            .Add('G', Sign)
            .Add('H', MultiplyByThousand)
            .Add('I', IncrementAwayFromZero)
            .Add('L', ShiftLeft)
            .Add('N', Minimum)
            .Add('R', ShiftRight)
            .Add('S', Sum)
            .Add('T', MultiplyByTen)
            .Add('U', UnsignedPrint)
            .Add('X', Maximum)
            .Add('Z', Negate)
            .BuildInstructions();

    static void Average(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        if (count == 0)
        {
            ctx.Push(0);
            return;
        }

        ctx.Push(SumValues(ctx, count) / count);
    }

    static void AbsoluteValue(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        ctx.Push(value == int.MinValue ? value : Math.Abs(value));
    }

    static void MultiplyByHundred(IFungeExecutionContext ctx) => Multiply(ctx, 100);

    static void DecrementTowardsZero(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        ctx.Push(value switch
        {
            > 0 => value - 1,
            < 0 => value + 1,
            _ => 0,
        });
    }

    static void MultiplyByTenThousand(IFungeExecutionContext ctx) => Multiply(ctx, 10000);

    static void Factorial(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (value < 0)
        {
            ctx.Reflect();
            return;
        }

        var result = 1;
        while (value > 0)
        {
            result = unchecked(result * value);
            value--;
        }

        ctx.Push(result);
    }

    static void Sign(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        ctx.Push(value switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        });
    }

    static void MultiplyByThousand(IFungeExecutionContext ctx) => Multiply(ctx, 1000);

    static void IncrementAwayFromZero(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        ctx.Push(value switch
        {
            > 0 => value + 1,
            < 0 => value - 1,
            _ => 0,
        });
    }

    static void ShiftLeft(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var value = ctx.Pop();
        var shift = NormalizeShiftCount(count);
        ctx.Push(count < 0 ? value >> shift : value << shift);
    }

    static void Minimum(IFungeExecutionContext ctx) => MinMax(ctx, isMaximum: false);

    static void ShiftRight(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var value = ctx.Pop();
        var shift = NormalizeShiftCount(count);
        ctx.Push(count < 0 ? value << shift : value >> shift);
    }

    static void Sum(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        ctx.Push(SumValues(ctx, count));
    }

    static void MultiplyByTen(IFungeExecutionContext ctx) => Multiply(ctx, 10);

    static void UnsignedPrint(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        output.WriteString(unchecked((uint)ctx.Pop()).ToString(CultureInfo.InvariantCulture));
        output.WriteString(" ");
    }

    static void Maximum(IFungeExecutionContext ctx) => MinMax(ctx, isMaximum: true);

    static void Negate(IFungeExecutionContext ctx)
        => ctx.Push(unchecked(-ctx.Pop()));

    static void Multiply(IFungeExecutionContext ctx, int factor)
        => ctx.Push(unchecked(ctx.Pop() * factor));

    static int SumValues(IFungeExecutionContext ctx, int count)
    {
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            sum = unchecked(sum + ctx.Pop());
        }

        return sum;
    }

    static void MinMax(IFungeExecutionContext ctx, bool isMaximum)
    {
        var count = ctx.Pop();
        if (count <= 0)
        {
            ctx.Reflect();
            return;
        }

        var result = ctx.Pop();
        for (var i = 1; i < count; i++)
        {
            var value = ctx.Pop();
            result = isMaximum ? Math.Max(result, value) : Math.Min(result, value);
        }

        ctx.Push(result);
    }

    static int NormalizeShiftCount(int count)
        => (int)(Math.Abs((long)count) % CellBitWidth);
}
