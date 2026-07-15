using System.Globalization;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Fpsp;

/// <summary>
/// Provides the standard Funge-98 <c>FPSP</c> fingerprint (handprint <c>0x46505350</c>).
/// </summary>
public sealed class SinglePrecisionFloatFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"FPSP"</c>.
    /// </summary>
    public const string NAME = "FPSP";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
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

    internal static float PopFloat(IFungeExecutionContext ctx)
        => FloatHelper.FromBits(ctx.Pop());

    internal static void PushFloat(IFungeExecutionContext ctx, float value)
        => ctx.Push(FloatHelper.ToBits(value));

    static void Add(IFungeExecutionContext ctx)
    {
        var y = PopFloat(ctx);
        var x = PopFloat(ctx);
        PushFloat(ctx, x + y);
    }

    static void Sin(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Sin(PopFloat(ctx)));
    static void Cos(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Cos(PopFloat(ctx)));

    static void Divide(IFungeExecutionContext ctx)
    {
        var y = PopFloat(ctx);
        var x = PopFloat(ctx);
        PushFloat(ctx, x / y);
    }

    static void Asin(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Asin(PopFloat(ctx)));
    static void FromInt(IFungeExecutionContext ctx) => PushFloat(ctx, (float)ctx.Pop());
    static void Atan(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Atan(PopFloat(ctx)));
    static void Acos(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Acos(PopFloat(ctx)));
    static void Truncate(IFungeExecutionContext ctx) => ctx.Push((int)(double)FloatHelper.Truncate(PopFloat(ctx)));
    static void Ln(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Log(PopFloat(ctx)));
    static void Log10(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Log10(PopFloat(ctx)));

    static void Multiply(IFungeExecutionContext ctx)
    {
        var y = PopFloat(ctx);
        var x = PopFloat(ctx);
        PushFloat(ctx, x * y);
    }

    static void Negate(IFungeExecutionContext ctx) => PushFloat(ctx, -PopFloat(ctx));

    static async ValueTask Print(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        await output.WriteStringAsync(PopFloat(ctx).ToString(CultureInfo.InvariantCulture));
    }

    static void Sqrt(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Sqrt(PopFloat(ctx)));

    static void Subtract(IFungeExecutionContext ctx)
    {
        var y = PopFloat(ctx);
        var x = PopFloat(ctx);
        PushFloat(ctx, x - y);
    }

    static void Tan(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Tan(PopFloat(ctx)));
    static void Abs(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Abs(PopFloat(ctx)));
    static void Exp(IFungeExecutionContext ctx) => PushFloat(ctx, FloatHelper.Exp(PopFloat(ctx)));

    static void Pow(IFungeExecutionContext ctx)
    {
        // Y: x_fp y_fp — (y^x)_fp: x on top
        var x = PopFloat(ctx);
        var y = PopFloat(ctx);
        PushFloat(ctx, FloatHelper.Pow(y, x));
    }
}
