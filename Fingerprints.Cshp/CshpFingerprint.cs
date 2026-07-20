using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Cshp;

/// <summary>
/// Provides a <c>CSHP</c> fingerprint (handprint <c>0x43534850</c>) for C# evaluation.
/// </summary>
public sealed class CshpFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"CSHP"</c>.
    /// </summary>
    public const string NAME = "CSHP";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('E', EvaluateString)
            .Add('I', EvaluateInt)
            .Add('S', IsAvailable)
            .BuildInstructions();

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

    static async ValueTask EvaluateString(IFungeExecutionContext ctx)
    {
        var code = Pop0gnirts(ctx);
        try
        {
            var value = await CSharpScript.EvaluateAsync<object?>(code).ConfigureAwait(false);
            Push0gnirts(ctx, value?.ToString() ?? string.Empty);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static async ValueTask EvaluateInt(IFungeExecutionContext ctx)
    {
        var code = Pop0gnirts(ctx);
        try
        {
            var value = await CSharpScript.EvaluateAsync<object?>(code).ConfigureAwait(false);
            if (TryToInt32(value, out var number))
            {
                ctx.Push(number);
                return;
            }

            ctx.Reflect();
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void IsAvailable(IFungeExecutionContext ctx) => ctx.Push(0);

    static bool TryToInt32(object? value, out int number)
    {
        switch (value)
        {
            case int i:
                number = i;
                return true;
            case null:
                number = 0;
                return true;
            case IConvertible convertible:
                try
                {
                    number = convertible.ToInt32(CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    number = 0;
                    return false;
                }
            default:
                number = 0;
                return false;
        }
    }
}
