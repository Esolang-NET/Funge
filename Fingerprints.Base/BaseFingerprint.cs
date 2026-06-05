namespace Esolang.Funge.Fingerprints.Base;

/// <summary>
/// Provides the standard Funge-98 <c>BASE</c> fingerprint (handprint <c>0x42415345</c>).
/// </summary>
public sealed class BaseFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("BASE");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="BaseFingerprint"/>.</summary>
    public BaseFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('B', OutputTopOfStackInBinary)
            .Add('H', OutputTopOfStackInHex)
            .Add('I', ReadInputInSpecifiedBase)
            .Add('N', OutputNInBaseB)
            .Add('O', OutputTopOfStackInOctal)
            .BuildInstructions();

    static void WriteInBase(IFungeExecutionContext ctx, int value, int numberBase)
        => ctx.WriteString((Convert.ToString(value, numberBase) ?? "0").ToUpperInvariant());

    /// <summary>
    /// B	(n -- )	Output top of stack in binary
    /// </summary>
    /// <param name="ctx"></param>
    static void OutputTopOfStackInBinary(IFungeExecutionContext ctx) => WriteInBase(ctx, ctx.Pop(), 2);

    /// <summary>
    /// H	(n -- )	Output top of stack in hex
    /// </summary>
    /// <param name="ctx"></param>
    static void OutputTopOfStackInHex(IFungeExecutionContext ctx) => WriteInBase(ctx, ctx.Pop(), 16);

    /// <summary>
    /// I	(b -- n)	Read input in specified base
    /// </summary>
    /// <param name="ctx"></param>
    static void ReadInputInSpecifiedBase(IFungeExecutionContext ctx)
    {
        var baseVal = ctx.Pop();
        var line = ctx.ReadLine();
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
    static void OutputNInBaseB(IFungeExecutionContext ctx)
    {
        var numberBase = ctx.Pop();
        var number = ctx.Pop();
        WriteInBase(ctx, number, numberBase);
    }

    /// <summary>
    /// O	(n -- )	Output top of stack in octal
    /// </summary>
    /// <param name="ctx"></param>
    static void OutputTopOfStackInOctal(IFungeExecutionContext ctx) => WriteInBase(ctx, ctx.Pop(), 8);


}
