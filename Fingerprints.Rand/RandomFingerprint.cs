using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Rand;

/// <summary>
/// Provides the standard Funge-98 <c>RAND</c> fingerprint (handprint <c>0x52414E44</c>).
/// </summary>
public sealed class RandomFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("RAND");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="RandomFingerprint"/>.</summary>
    public RandomFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('I', IntegerRandom)
            .Add('M', MaximumInteger)
            .Add('R', FloatRandom)
            .Add('S', Seed)
            .Add('T', TimeSeed)
            .BuildInstructions();

    static bool TryGetRandomContext(IFungeExecutionContext ctx, out IFungeRandomContext random)
    {
        if (ctx is not IFungeRandomContext foundRandom)
        {
            random = null!;
            ctx.Reflect();
            return false;
        }

        random = foundRandom;
        return true;
    }

    static void IntegerRandom(IFungeExecutionContext ctx)
    {
        if (!TryGetRandomContext(ctx, out var random))
            return;

        var upperBound = unchecked((uint)ctx.Pop());
        if (upperBound == 0)
        {
            ctx.Reflect();
            return;
        }

        ctx.Push(unchecked((int)random.NextUInt32(upperBound)));
    }

    static void MaximumInteger(IFungeExecutionContext ctx)
        => ctx.Push(int.MaxValue);

    static void FloatRandom(IFungeExecutionContext ctx)
    {
        if (!TryGetRandomContext(ctx, out var random))
            return;

        ctx.Push(BitConverter.ToInt32(BitConverter.GetBytes(random.NextSingle()), 0));
    }

    static void Seed(IFungeExecutionContext ctx)
    {
        if (!TryGetRandomContext(ctx, out var random))
            return;

        random.Reseed(unchecked((uint)ctx.Pop()));
    }

    static void TimeSeed(IFungeExecutionContext ctx)
    {
        if (!TryGetRandomContext(ctx, out var random))
            return;

        random.Reseed();
    }
}
