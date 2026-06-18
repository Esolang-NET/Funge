using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Fixp;

/// <summary>
/// Provides the standard Funge-98 <c>FIXP</c> fingerprint (handprint <c>0x46495850</c>).
/// </summary>
public sealed class FixedPointFingerprint : IFingerprint
{
    const int Scale = 10000;
    const double DegreesPerRadian = 180.0 / Math.PI;
    const double RadiansPerDegree = Math.PI / 180.0;

    readonly object _randomLock = new();
    readonly Random _random = new();

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("FIXP");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="FixedPointFingerprint"/>.</summary>
    public FixedPointFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', BitwiseAnd)
            .Add('B', ArcCosine)
            .Add('C', Cosine)
            .Add('D', RandomInteger)
            .Add('I', Sine)
            .Add('J', ArcSine)
            .Add('N', Negate)
            .Add('O', BitwiseOr)
            .Add('P', MultiplyByPi)
            .Add('Q', SquareRoot)
            .Add('R', Power)
            .Add('S', Sign)
            .Add('T', Tangent)
            .Add('U', ArcTangent)
            .Add('V', AbsoluteValue)
            .Add('X', BitwiseExclusiveOr)
            .BuildInstructions();

    static void BitwiseAnd(IFungeExecutionContext ctx)
    {
        var right = ctx.Pop();
        var left = ctx.Pop();
        ctx.Push(left & right);
    }

    static void ArcCosine(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (!TryGetUnitInput(ctx, value, out var normalized))
            return;

        TryPushRounded(ctx, Math.Acos(normalized) * DegreesPerRadian * Scale);
    }

    static void Cosine(IFungeExecutionContext ctx)
        => TryPushRounded(ctx, Math.Cos(ToRadians(ctx.Pop())) * Scale);

    void RandomInteger(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (value == 0)
        {
            ctx.Push(0);
            return;
        }

        lock (_randomLock)
        {
            ctx.Push(value > 0 ? _random.Next(value) : value + _random.Next(-value));
        }
    }

    static void Sine(IFungeExecutionContext ctx)
        => TryPushRounded(ctx, Math.Sin(ToRadians(ctx.Pop())) * Scale);

    static void ArcSine(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (!TryGetUnitInput(ctx, value, out var normalized))
            return;

        TryPushRounded(ctx, Math.Asin(normalized) * DegreesPerRadian * Scale);
    }

    static void Negate(IFungeExecutionContext ctx)
        => ctx.Push(unchecked(-ctx.Pop()));

    static void BitwiseOr(IFungeExecutionContext ctx)
    {
        var right = ctx.Pop();
        var left = ctx.Pop();
        ctx.Push(left | right);
    }

    static void MultiplyByPi(IFungeExecutionContext ctx)
        => TryPushTruncated(ctx, Math.PI * ctx.Pop());

    static void SquareRoot(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (value < 0)
        {
            ctx.Reflect();
            return;
        }

        TryPushTruncated(ctx, Math.Sqrt(value));
    }

    static void Power(IFungeExecutionContext ctx)
    {
        var exponent = ctx.Pop();
        var basis = ctx.Pop();
        if (basis == 0 && exponent <= 0)
        {
            ctx.Reflect();
            return;
        }

        if (exponent < 0)
        {
            ctx.Push(basis switch
            {
                1 => 1,
                -1 => (exponent & 1) == 0 ? 1 : -1,
                _ => 0,
            });
            return;
        }

        var result = 1;
        var power = basis;
        var remaining = exponent;
        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
                result = unchecked(result * power);

            remaining >>= 1;
            if (remaining != 0)
                power = unchecked(power * power);
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

    static void Tangent(IFungeExecutionContext ctx)
    {
        var radians = ToRadians(ctx.Pop());
        if (Math.Abs(Math.Cos(radians)) < 1e-12)
        {
            ctx.Reflect();
            return;
        }

        TryPushRounded(ctx, Math.Tan(radians) * Scale);
    }

    static void ArcTangent(IFungeExecutionContext ctx)
        => TryPushRounded(ctx, Math.Atan(ctx.Pop() / (double)Scale) * DegreesPerRadian * Scale);

    static void AbsoluteValue(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        ctx.Push(value == int.MinValue ? value : Math.Abs(value));
    }

    static void BitwiseExclusiveOr(IFungeExecutionContext ctx)
    {
        var right = ctx.Pop();
        var left = ctx.Pop();
        ctx.Push(left ^ right);
    }

    static double ToRadians(int scaledDegrees)
        => scaledDegrees / (double)Scale * RadiansPerDegree;

    static bool TryGetUnitInput(IFungeExecutionContext ctx, int value, out double normalized)
    {
        if (value is < -Scale or > Scale)
        {
            normalized = 0;
            ctx.Reflect();
            return false;
        }

        normalized = value / (double)Scale;
        return true;
    }

    static void TryPushRounded(IFungeExecutionContext ctx, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < int.MinValue || value > int.MaxValue)
        {
            ctx.Reflect();
            return;
        }

        ctx.Push((int)Math.Round(value, MidpointRounding.AwayFromZero));
    }

    static void TryPushTruncated(IFungeExecutionContext ctx, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < int.MinValue || value > int.MaxValue)
        {
            ctx.Reflect();
            return;
        }

        ctx.Push((int)Math.Truncate(value));
    }
}
