using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Orth;

/// <summary>
/// Provides the standard Funge-98 <c>ORTH</c> fingerprint (handprint <c>0x4F525448</c>).
/// </summary>
public sealed class OrthogonalFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"ORTH"</c>.
    /// </summary>
    public const string NAME = "ORTH";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('A', BitwiseAnd)
            .Add('E', BitwiseXor)
            .Add('G', GetCell)
            .Add('O', BitwiseOr)
            .Add('P', PutCell)
            .Add('S', WriteString)
            .Add('V', SetDeltaX)
            .Add('W', SetDeltaY)
            .Add('X', SetPositionX)
            .Add('Y', SetPositionY)
            .Add('Z', SkipIfZero)
            .BuildInstructions();

    static void BitwiseAnd(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(a & b);
    }

    static void BitwiseXor(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(a ^ b);
    }

    static void BitwiseOr(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(a | b);
    }

    static void GetCell(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeSpaceContext space)
        {
            ctx.Reflect();
            return;
        }

        var x = ctx.Pop();
        var y = ctx.Pop();
        ctx.Push(space.GetCell(x, y, 0));
    }

    static void PutCell(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeSpaceContext space)
        {
            ctx.Reflect();
            return;
        }

        var value = ctx.Pop();
        var x = ctx.Pop();
        var y = ctx.Pop();
        space.SetCell(x, y, 0, value);
    }

    static async ValueTask WriteString(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        var chars = new List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        await output.WriteStringAsync(new string([.. chars]));
    }

    static void SetDeltaX(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        var dx = ctx.Pop();
        var (_, dy, dz) = pos.Delta;
        pos.Delta = (dx, dy, dz);
    }

    static void SetDeltaY(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        if (ctx is IFungeSpaceContext space && space.Dimensions < 2)
        {
            ctx.Reflect();
            return;
        }

        var dy = ctx.Pop();
        var (dx, _, dz) = pos.Delta;
        pos.Delta = (dx, dy, dz);
    }

    static void SetPositionX(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        var x = ctx.Pop();
        var (_, py, pz) = pos.Position;
        pos.Position = (x, py, pz);
    }

    static void SetPositionY(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        if (ctx is IFungeSpaceContext space && space.Dimensions < 2)
        {
            ctx.Reflect();
            return;
        }

        var y = ctx.Pop();
        var (px, _, pz) = pos.Position;
        pos.Position = (px, y, pz);
    }

    static void SkipIfZero(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        var a = ctx.Pop();
        if (a == 0)
        {
            var (px, py, pz) = pos.Position;
            var (dx, dy, dz) = pos.Delta;
            pos.Position = (px + dx, py + dy, pz + dz);
        }
    }
}
