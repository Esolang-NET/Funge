using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Base;

/// <summary>
/// Provides the standard Funge-98 <c>BASE</c> fingerprint (handprint <c>0x42415345</c>).
/// </summary>
public sealed class BaseFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"BASE"</c>.
    /// </summary>
    public const string NAME = "BASE";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('B', OutputTopOfStackInBinary)
            .Add('H', OutputTopOfStackInHex)
            .Add('I', ReadInputInSpecifiedBase)
            .Add('N', OutputNInBaseB)
            .Add('O', OutputTopOfStackInOctal)
            .BuildInstructions();

    static async ValueTask WriteInBase(IFungeOutputContext output, int value, int numberBase)
        => await output.WriteStringAsync((Convert.ToString(value, numberBase) ?? "0").ToUpperInvariant());

    /// <summary>
    /// B	(n -- )	Output top of stack in binary
    /// </summary>
    /// <param name="ctx"></param>
    static async ValueTask OutputTopOfStackInBinary(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        await WriteInBase(output, ctx.Pop(), 2);
    }

    /// <summary>
    /// H	(n -- )	Output top of stack in hex
    /// </summary>
    /// <param name="ctx"></param>
    static async ValueTask OutputTopOfStackInHex(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        await WriteInBase(output, ctx.Pop(), 16);
    }

    /// <summary>
    /// I	(b -- n)	Read input in specified base
    /// </summary>
    /// <param name="ctx"></param>
    static async ValueTask ReadInputInSpecifiedBase(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeInputContext input)
        {
            ctx.Reflect();
            return;
        }

        var baseVal = ctx.Pop();
        var line = await input.ReadLineAsync();
        if (line is null)
        {
            ctx.Reflect();
            return;
        }

        try
        {
            ctx.Push(Convert.ToInt32(line.Trim(), baseVal));
        }
        catch
        {
            ctx.Push(0);
        }
    }

    /// <summary>
    /// N	(n b -- )	Output n in base b
    /// </summary>
    /// <param name="ctx"></param>
    static async ValueTask OutputNInBaseB(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        var numberBase = ctx.Pop();
        var number = ctx.Pop();
        await WriteInBase(output, number, numberBase);
    }

    /// <summary>
    /// O	(n -- )	Output top of stack in octal
    /// </summary>
    /// <param name="ctx"></param>
    static async ValueTask OutputTopOfStackInOctal(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        await WriteInBase(output, ctx.Pop(), 8);
    }


}
