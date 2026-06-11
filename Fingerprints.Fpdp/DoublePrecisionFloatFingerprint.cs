using System.Globalization;

namespace Esolang.Funge.Fingerprints.Fpdp;

/// <summary>
/// Provides the standard Funge-98 <c>FPDP</c> fingerprint (handprint <c>0x46504450</c>).
/// </summary>
public sealed class DoublePrecisionFloatFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("FPDP");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="DoublePrecisionFloatFingerprint"/>.</summary>
    public DoublePrecisionFloatFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', Add)
            .Add('B', Sin)
            .Add('C', Cos)
            .Add('D', Divide)
            .Add('E', Asin)
            .Add('F', FromInt)
            .Add('G', Atan)
            .Add('H', Acos)
            .Add('I', Truncate)
            .Add('K', Ln)
            .Add('L', Log10)
            .Add('M', Multiply)
            .Add('N', Negate)
            .Add('P', Print)
            .Add('Q', Sqrt)
            .Add('S', Subtract)
            .Add('T', Tan)
            .Add('V', Abs)
            .Add('X', Exp)
            .Add('Y', Pow)
            .BuildInstructions();

    // Two cells: push lower 32 bits first, upper 32 bits second.
    // Pop: pop upper first, then lower.
    static double PopDouble(IFungeExecutionContext ctx)
    {
        var upper = ctx.Pop();
        var lower = ctx.Pop();
        var bits = ((long)upper << 32) | unchecked((uint)lower);
        return BitConverter.Int64BitsToDouble(bits);
    }

    static void PushDouble(IFungeExecutionContext ctx, double value)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        ctx.Push(unchecked((int)(bits & 0xFFFFFFFFL)));
        ctx.Push(unchecked((int)(bits >> 32)));
    }

    static void Add(IFungeExecutionContext ctx)
    {
        var y = PopDouble(ctx);
        var x = PopDouble(ctx);
        PushDouble(ctx, x + y);
    }

    static void Sin(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Sin(PopDouble(ctx)));
    static void Cos(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Cos(PopDouble(ctx)));

    static void Divide(IFungeExecutionContext ctx)
    {
        var y = PopDouble(ctx);
        var x = PopDouble(ctx);
        PushDouble(ctx, x / y);
    }

    static void Asin(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Asin(PopDouble(ctx)));
    static void FromInt(IFungeExecutionContext ctx) => PushDouble(ctx, (double)ctx.Pop());
    static void Atan(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Atan(PopDouble(ctx)));
    static void Acos(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Acos(PopDouble(ctx)));
    static void Truncate(IFungeExecutionContext ctx) => ctx.Push((int)Math.Truncate(PopDouble(ctx)));
    static void Ln(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Log(PopDouble(ctx)));
    static void Log10(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Log10(PopDouble(ctx)));

    static void Multiply(IFungeExecutionContext ctx)
    {
        var y = PopDouble(ctx);
        var x = PopDouble(ctx);
        PushDouble(ctx, x * y);
    }

    static void Negate(IFungeExecutionContext ctx) => PushDouble(ctx, -PopDouble(ctx));

    static void Print(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        output.WriteString(PopDouble(ctx).ToString(CultureInfo.InvariantCulture));
    }

    static void Sqrt(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Sqrt(PopDouble(ctx)));

    static void Subtract(IFungeExecutionContext ctx)
    {
        var y = PopDouble(ctx);
        var x = PopDouble(ctx);
        PushDouble(ctx, x - y);
    }

    static void Tan(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Tan(PopDouble(ctx)));
    static void Abs(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Abs(PopDouble(ctx)));
    static void Exp(IFungeExecutionContext ctx) => PushDouble(ctx, Math.Exp(PopDouble(ctx)));

    static void Pow(IFungeExecutionContext ctx)
    {
        // Y: x y — (y^x): x on top
        var x = PopDouble(ctx);
        var y = PopDouble(ctx);
        PushDouble(ctx, Math.Pow(y, x));
    }
}
