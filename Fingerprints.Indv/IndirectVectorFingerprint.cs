using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Indv;

/// <summary>
/// Provides the standard Funge-98 <c>INDV</c> fingerprint (handprint <c>0x494E4456</c>).
/// </summary>
public sealed class IndirectVectorFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"INDV"</c>.
    /// </summary>
    public const string NAME = "INDV";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('G', GetNumber)
            .Add('P', PutNumber)
            .Add('V', GetVector)
            .Add('W', PutVector)
            .BuildInstructions();

    static bool TryGetRequiredContexts(
        IFungeExecutionContext ctx,
        out IFungeVectorContext vector,
        out IFungeSpaceContext space,
        out IFungeStorageOffsetContext offset)
    {
        if (ctx is not IFungeVectorContext foundVector
            || ctx is not IFungeSpaceContext foundSpace
            || ctx is not IFungeStorageOffsetContext foundOffset)
        {
            vector = null!;
            space = null!;
            offset = null!;
            ctx.Reflect();
            return false;
        }

        vector = foundVector;
        space = foundSpace;
        offset = foundOffset;
        return true;
    }

    static int NormalizeDimensions(IFungeSpaceContext space)
        => space.Dimensions <= 1 ? 1 : space.Dimensions == 2 ? 2 : 3;

    static (int X, int Y, int Z) Add((int X, int Y, int Z) left, (int X, int Y, int Z) right)
        => (left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    static (int X, int Y, int Z) ReadStoredVector(IFungeSpaceContext space, (int X, int Y, int Z) origin)
        => NormalizeDimensions(space) switch
        {
            1 => (space.GetCell(origin.X, origin.Y, origin.Z), 0, 0),
            2 => (
                space.GetCell(origin.X + 1, origin.Y, origin.Z),
                space.GetCell(origin.X, origin.Y, origin.Z),
                0),
            _ => (
                space.GetCell(origin.X + 2, origin.Y, origin.Z),
                space.GetCell(origin.X + 1, origin.Y, origin.Z),
                space.GetCell(origin.X, origin.Y, origin.Z)),
        };

    static void WriteStoredVector(IFungeSpaceContext space, (int X, int Y, int Z) origin, (int X, int Y, int Z) value)
    {
        switch (NormalizeDimensions(space))
        {
            case 1:
                space.SetCell(origin.X, origin.Y, origin.Z, value.X);
                break;
            case 2:
                space.SetCell(origin.X, origin.Y, origin.Z, value.Y);
                space.SetCell(origin.X + 1, origin.Y, origin.Z, value.X);
                break;
            default:
                space.SetCell(origin.X, origin.Y, origin.Z, value.Z);
                space.SetCell(origin.X + 1, origin.Y, origin.Z, value.Y);
                space.SetCell(origin.X + 2, origin.Y, origin.Z, value.X);
                break;
        }
    }

    static (int X, int Y, int Z) ResolveIndirectTarget(
        IFungeVectorContext vector,
        IFungeSpaceContext space,
        IFungeStorageOffsetContext offset)
    {
        var pointer = vector.PopVector();
        var pointerOrigin = Add(pointer, offset.StorageOffset);
        var target = ReadStoredVector(space, pointerOrigin);
        return Add(target, offset.StorageOffset);
    }

    static void GetNumber(IFungeExecutionContext ctx)
    {
        if (!TryGetRequiredContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (targetX, targetY, targetZ) = ResolveIndirectTarget(vector, space, offset);
        ctx.Push(space.GetCell(targetX, targetY, targetZ));
    }

    static void PutNumber(IFungeExecutionContext ctx)
    {
        if (!TryGetRequiredContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (targetX, targetY, targetZ) = ResolveIndirectTarget(vector, space, offset);
        var value = ctx.Pop();
        space.SetCell(targetX, targetY, targetZ, value);
    }

    static void GetVector(IFungeExecutionContext ctx)
    {
        if (!TryGetRequiredContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (x, y, z) = ReadStoredVector(space, ResolveIndirectTarget(vector, space, offset));
        vector.PushVector(x, y, z);
    }

    static void PutVector(IFungeExecutionContext ctx)
    {
        if (!TryGetRequiredContexts(ctx, out var vector, out var space, out var offset))
            return;

        var target = ResolveIndirectTarget(vector, space, offset);
        var value = vector.PopVector();
        WriteStoredVector(space, target, value);
    }
}
