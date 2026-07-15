using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Frth;

/// <summary>
/// Provides the standard Funge-98 <c>FRTH</c> fingerprint (handprint <c>0x46525448</c>).
/// </summary>
public sealed class ForthFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"FRTH"</c>.
    /// </summary>
    public const string NAME = "FRTH";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('D', StackDepth)
            .Add('L', Roll)
            .Add('O', Over)
            .Add('P', Pick)
            .Add('R', Rot)
            .BuildInstructions();

    static bool TryGetStackContext(IFungeExecutionContext ctx, out IFungeStackContext stack)
    {
        if (ctx is not IFungeStackContext foundStack)
        {
            stack = null!;
            ctx.Reflect();
            return false;
        }

        stack = foundStack;
        return true;
    }

    static List<int> SnapshotTopStack(IFungeExecutionContext ctx, int depth)
    {
        List<int> values = [];
        for (var i = 0; i < depth; i++)
            values.Add(ctx.Pop());

        for (var i = values.Count - 1; i >= 0; i--)
            ctx.Push(values[i]);

        return values;
    }

    static void ReplaceTopStack(IFungeExecutionContext ctx, int originalDepth, IReadOnlyList<int> topFirstValues)
    {
        for (var i = 0; i < originalDepth; i++)
            ctx.Pop();

        for (var i = topFirstValues.Count - 1; i >= 0; i--)
            ctx.Push(topFirstValues[i]);
    }

    static void StackDepth(IFungeExecutionContext ctx)
    {
        if (!TryGetStackContext(ctx, out var stack))
            return;

        ctx.Push(stack.StackDepth);
    }

    static void Over(IFungeExecutionContext ctx)
    {
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(a);
        ctx.Push(b);
        ctx.Push(a);
    }

    static void Rot(IFungeExecutionContext ctx)
    {
        var c = ctx.Pop();
        var b = ctx.Pop();
        var a = ctx.Pop();
        ctx.Push(b);
        ctx.Push(c);
        ctx.Push(a);
    }

    static void Pick(IFungeExecutionContext ctx)
    {
        var position = ctx.Pop();
        if (position < 0)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetStackContext(ctx, out var stack))
            return;

        if (position >= stack.StackDepth)
        {
            ctx.Push(0);
            return;
        }

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        ctx.Push(values[position]);
    }

    static void Roll(IFungeExecutionContext ctx)
    {
        var amount = ctx.Pop();
        if (!TryGetStackContext(ctx, out var stack))
            return;

        if (stack.StackDepth == 0)
        {
            ctx.Push(0);
            return;
        }

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        if (amount >= 0)
        {
            if (amount >= values.Count)
            {
                ctx.Push(0);
                return;
            }

            var picked = values[amount];
            values.RemoveAt(amount);
            values.Insert(0, picked);
            ReplaceTopStack(ctx, stack.StackDepth, values);
            return;
        }

        var depth = -amount;
        var top = values[0];
        values.RemoveAt(0);

        while (values.Count < depth - 1)
            values.Add(0);

        values.Insert(depth - 1, top);
        ReplaceTopStack(ctx, stack.StackDepth, values);
    }
}
