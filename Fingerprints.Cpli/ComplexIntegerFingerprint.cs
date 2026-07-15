using System.Globalization;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Cpli;

/// <summary>
/// Provides the standard Funge-98 <c>CPLI</c> fingerprint (handprint <c>0x43504C49</c>).
/// </summary>
public sealed class ComplexIntegerFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"CPLI"</c>.
    /// </summary>
    public const string NAME = "CPLI";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('A', Add)
            .Add('D', Divide)
            .Add('M', Multiply)
            .Add('O', Output)
            .Add('S', Subtract)
            .Add('V', Magnitude)
            .BuildInstructions();

    // Stack layout: real (lower), imag (upper).
    // For binary ops: pop d(imag2), c(real2), b(imag1), a(real1).
    static (int real, int imag) PopComplex(IFungeExecutionContext ctx)
    {
        var imag = ctx.Pop();
        var real = ctx.Pop();
        return (real, imag);
    }

    static void PushComplex(IFungeExecutionContext ctx, int real, int imag)
    {
        ctx.Push(real);
        ctx.Push(imag);
    }

    static void Add(IFungeExecutionContext ctx)
    {
        var (c, d) = PopComplex(ctx);
        var (a, b) = PopComplex(ctx);
        PushComplex(ctx, unchecked(a + c), unchecked(b + d));
    }

    static void Subtract(IFungeExecutionContext ctx)
    {
        var (c, d) = PopComplex(ctx);
        var (a, b) = PopComplex(ctx);
        PushComplex(ctx, unchecked(a - c), unchecked(b - d));
    }

    static void Multiply(IFungeExecutionContext ctx)
    {
        var (c, d) = PopComplex(ctx);
        var (a, b) = PopComplex(ctx);
        // (a+bi)*(c+di) = (ac-bd) + (ad+bc)i
        PushComplex(ctx, unchecked(a * c - b * d), unchecked(a * d + b * c));
    }

    static void Divide(IFungeExecutionContext ctx)
    {
        var (c, d) = PopComplex(ctx);
        var (a, b) = PopComplex(ctx);
        if (c == 0 && d == 0)
        {
            ctx.Reflect();
            return;
        }

        var denom = c * c + d * d;
        var realPart = (int)Math.Truncate((double)(a * c + b * d) / denom);
        var imagPart = (int)Math.Truncate((double)(b * c - a * d) / denom);
        PushComplex(ctx, realPart, imagPart);
    }

    static async ValueTask Output(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        var b = ctx.Pop();
        var a = ctx.Pop();
        await output.WriteStringAsync(string.Format(CultureInfo.InvariantCulture, "{0}+{1}i", a, b));
    }

    static void Magnitude(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push((int)Math.Sqrt((double)a * a + (double)b * b));
    }
}
