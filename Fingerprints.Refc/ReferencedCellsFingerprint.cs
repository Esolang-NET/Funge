using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Refc;

/// <summary>
/// Provides the standard Funge-98 <c>REFC</c> fingerprint (handprint <c>0x52454643</c>).
/// </summary>
public sealed class ReferencedCellsFingerprint(string? name = null) : IFingerprint
{
    readonly Dictionary<int, (int X, int Y, int Z)> _table = [];
    int _nextRef;
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"REFC"</c>.
    /// </summary>
    public const string NAME = "REFC";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
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
