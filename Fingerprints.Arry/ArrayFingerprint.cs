namespace Esolang.Funge.Fingerprints.Arry;

/// <summary>
/// Provides the standard Funge-98 <c>ARRY</c> fingerprint (handprint <c>0x41525259</c>).
/// </summary>
public sealed class ArrayFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("ARRY");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="ArrayFingerprint"/>.</summary>
    public ArrayFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', Store1D)
            .Add('B', Retrieve1D)
            .Add('C', Store2D)
            .Add('D', Retrieve2D)
            .Add('E', Store3D)
            .Add('F', Retrieve3D)
            .Add('G', GetMaximumDimensions)
            .BuildInstructions();

    static bool TryGetSpaceAndVectorContexts(
        IFungeExecutionContext ctx,
        out IFungeSpaceContext space,
        out IFungeVectorContext vector)
    {
        if (ctx is not IFungeSpaceContext foundSpace || ctx is not IFungeVectorContext foundVector)
        {
            space = null!;
            vector = null!;
            ctx.Reflect();
            return false;
        }

        space = foundSpace;
        vector = foundVector;
        return true;
    }

    static void GetMaximumDimensions(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeSpaceContext space)
        {
            ctx.Reflect();
            return;
        }

        ctx.Push(space.Dimensions);
    }

    static void Store1D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexX = ctx.Pop();
        var value = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        space.SetCell(x + indexX, y, z, value);
        vector.PushVector(x, y, z);
    }

    static void Retrieve1D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexX = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        vector.PushVector(x, y, z);
        ctx.Push(space.GetCell(x + indexX, y, z));
    }

    static void Store2D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexY = ctx.Pop();
        var indexX = ctx.Pop();
        var value = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        space.SetCell(x + indexX, y + indexY, z, value);
        vector.PushVector(x, y, z);
    }

    static void Retrieve2D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexY = ctx.Pop();
        var indexX = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        vector.PushVector(x, y, z);
        ctx.Push(space.GetCell(x + indexX, y + indexY, z));
    }

    static void Store3D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexZ = ctx.Pop();
        var indexY = ctx.Pop();
        var indexX = ctx.Pop();
        var value = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        space.SetCell(x + indexX, y + indexY, z + indexZ, value);
        vector.PushVector(x, y, z);
    }

    static void Retrieve3D(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceAndVectorContexts(ctx, out var space, out var vector))
            return;

        var indexZ = ctx.Pop();
        var indexY = ctx.Pop();
        var indexX = ctx.Pop();
        var (x, y, z) = vector.PopVector();
        vector.PushVector(x, y, z);
        ctx.Push(space.GetCell(x + indexX, y + indexY, z + indexZ));
    }
}
