using System.Globalization;
using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Sets;

/// <summary>
/// Provides the standard Funge-98 <c>SETS</c> fingerprint (handprint <c>0x53455453</c>).
/// </summary>
public sealed class SetOperationsFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("SETS");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="SetOperationsFingerprint"/>.</summary>
    public SetOperationsFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', AddElement)
            .Add('C', Count)
            .Add('D', Duplicate)
            .Add('G', GetFromSpace)
            .Add('I', Intersect)
            .Add('M', Member)
            .Add('P', Print)
            .Add('R', RemoveElement)
            .Add('S', Subtract)
            .Add('U', Union)
            .Add('W', WriteToSpace)
            .Add('X', Exchange)
            .Add('Z', Discard)
            .BuildInstructions();

    // Sets on stack: elements (any order) followed by count on top.
    static bool TryPopSet(IFungeExecutionContext ctx, out HashSet<int> set)
    {
        var count = ctx.Pop();
        if (count < 0)
        {
            set = null!;
            ctx.Reflect();
            return false;
        }

        set = [];
        for (var i = 0; i < count; i++)
        {
            var elem = ctx.Pop();
            if (!set.Add(elem))
            {
                // duplicates: reflect
                ctx.Reflect();
                return false;
            }
        }

        return true;
    }

    static void PushSet(IFungeExecutionContext ctx, HashSet<int> set)
    {
        foreach (var elem in set)
            ctx.Push(elem);
        ctx.Push(set.Count);
    }

    static void AddElement(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (!TryPopSet(ctx, out var set))
            return;
        set.Add(value);
        PushSet(ctx, set);
    }

    static void Count(IFungeExecutionContext ctx)
    {
        // Peek at top count without consuming set
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        // Collect elements, then re-push
        var elems = new int[count];
        for (var i = 0; i < count; i++)
            elems[i] = ctx.Pop();

        // Push set back
        for (var i = count - 1; i >= 0; i--)
            ctx.Push(elems[i]);
        ctx.Push(count);

        // Push count again
        ctx.Push(count);
    }

    static void Duplicate(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var set))
            return;
        PushSet(ctx, set);
        PushSet(ctx, [.. set]);
    }

    static void GetFromSpace(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeVectorContext vector || ctx is not IFungeSpaceContext space)
        {
            ctx.Reflect();
            return;
        }

        var source = vector.PopVector();
        var delta = vector.PopVector();

        var count = space.GetCell(source.X, source.Y, source.Z);
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        HashSet<int> set = [];
        var (x, y, z) = source;
        var (dx, dy, dz) = delta;
        x += dx; y += dy; z += dz;
        for (var i = 0; i < count; i++)
        {
            var elem = space.GetCell(x, y, z);
            if (!set.Add(elem))
            {
                ctx.Reflect();
                return;
            }

            x += dx; y += dy; z += dz;
        }

        PushSet(ctx, set);
    }

    static void Intersect(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var b) || !TryPopSet(ctx, out var a))
            return;
        a.IntersectWith(b);
        PushSet(ctx, a);
    }

    static void Member(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var set))
            return;
        var value = ctx.Pop();
        ctx.Push(set.Contains(value) ? 1 : 0);
    }

    static async ValueTask Print(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeOutputContext output)
        {
            ctx.Reflect();
            return;
        }

        if (!TryPopSet(ctx, out var set))
            return;

        var sb = new StringBuilder("{");
        var first = true;
        foreach (var elem in set)
        {
            if (!first) sb.Append(", ");
            sb.Append(elem.ToString(CultureInfo.InvariantCulture));
            first = false;
        }

        sb.Append('}');
        await output.WriteStringAsync(sb.ToString());
    }

    static void RemoveElement(IFungeExecutionContext ctx)
    {
        var value = ctx.Pop();
        if (!TryPopSet(ctx, out var set))
            return;
        set.Remove(value);
        PushSet(ctx, set);
    }

    static void Subtract(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var b) || !TryPopSet(ctx, out var a))
            return;
        a.ExceptWith(b);
        PushSet(ctx, a);
    }

    static void Union(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var b) || !TryPopSet(ctx, out var a))
            return;
        a.UnionWith(b);
        PushSet(ctx, a);
    }

    static void WriteToSpace(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungeVectorContext vector || ctx is not IFungeSpaceContext space)
        {
            ctx.Reflect();
            return;
        }

        var dest = vector.PopVector();
        var delta = vector.PopVector();

        if (!TryPopSet(ctx, out var set))
            return;

        var (x, y, z) = dest;
        var (dx, dy, dz) = delta;
        space.SetCell(x, y, z, set.Count);
        x += dx; y += dy; z += dz;
        foreach (var elem in set)
        {
            space.SetCell(x, y, z, elem);
            x += dx; y += dy; z += dz;
        }

        PushSet(ctx, set);
    }

    static void Exchange(IFungeExecutionContext ctx)
    {
        if (!TryPopSet(ctx, out var b) || !TryPopSet(ctx, out var a))
            return;
        PushSet(ctx, b);
        PushSet(ctx, a);
    }

    static void Discard(IFungeExecutionContext ctx)
        => TryPopSet(ctx, out _);
}
