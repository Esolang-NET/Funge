namespace Esolang.Funge.Fingerprints.Refc;

/// <summary>
/// Provides the standard Funge-98 <c>REFC</c> fingerprint (handprint <c>0x52454643</c>).
/// </summary>
public sealed class ReferencedCellsFingerprint : IFingerprint
{
    readonly Dictionary<int, (int X, int Y, int Z)> _table = [];
    int _nextRef;

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("REFC");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="ReferencedCellsFingerprint"/>.</summary>
    public ReferencedCellsFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('R', Remember)
            .Add('D', Dereference)
            .BuildInstructions();

    void Remember(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeVectorContext vector)
        {
            ctx.Reflect();
            return;
        }

        var v = vector.PopVector();
        var refId = _nextRef++;
        _table[refId] = v;
        ctx.Push(refId);
    }

    void Dereference(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeVectorContext vector)
        {
            ctx.Reflect();
            return;
        }

        var refId = ctx.Pop();
        if (!_table.TryGetValue(refId, out var v))
        {
            ctx.Reflect();
            return;
        }

        vector.PushVector(v.X, v.Y, v.Z);
    }
}
