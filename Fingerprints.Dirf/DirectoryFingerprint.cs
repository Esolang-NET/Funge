namespace Esolang.Funge.Fingerprints.Dirf;

/// <summary>
/// Provides the standard Funge-98 <c>DIRF</c> fingerprint (handprint <c>0x44495246</c>).
/// </summary>
public sealed class DirectoryFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("DIRF");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="DirectoryFingerprint"/>.</summary>
    public DirectoryFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('C', ChangeDirectory)
            .Add('M', MakeDirectory)
            .Add('R', RemoveDirectory)
            .BuildInstructions();

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var chars = new System.Collections.Generic.List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        return new string([.. chars]);
    }

    static void ChangeDirectory(IFungeExecutionContext ctx)
    {
        var path = Pop0gnirts(ctx);
        try
        {
            System.IO.Directory.SetCurrentDirectory(path);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void MakeDirectory(IFungeExecutionContext ctx)
    {
        var path = Pop0gnirts(ctx);
        try
        {
            System.IO.Directory.CreateDirectory(path);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void RemoveDirectory(IFungeExecutionContext ctx)
    {
        var path = Pop0gnirts(ctx);
        try
        {
            System.IO.Directory.Delete(path);
        }
        catch
        {
            ctx.Reflect();
        }
    }
}
