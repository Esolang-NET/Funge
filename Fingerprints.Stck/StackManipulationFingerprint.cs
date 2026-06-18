using System.Globalization;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.Stck;

/// <summary>
/// Provides the standard Funge-98 <c>STCK</c> fingerprint (handprint <c>0x5354434B</c>).
/// </summary>
public sealed class StackManipulationFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("STCK");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="StackManipulationFingerprint"/>.</summary>
    public StackManipulationFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('B', Bury)
            .Add('C', Count)
            .Add('D', DuplicateTopValues)
            .Add('G', GetFromSpace)
            .Add('K', MoveBlockToTop)
            .Add('N', ReverseTopValues)
            .Add('P', PrintStack)
            .Add('R', ReverseAll)
            .Add('S', DuplicateSecond)
            .Add('T', SwapSecondAndThird)
            .Add('U', DropUntilFound)
            .Add('W', WriteToSpace)
            .Add('Z', ReverseZeroTerminatedString)
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

    static bool TryGetOutputStackContext(IFungeExecutionContext ctx, out IFungeOutputContext output, out IFungeStackContext stack)
    {
        if (ctx is not IFungeOutputContext foundOutput || ctx is not IFungeStackContext foundStack)
        {
            output = null!;
            stack = null!;
            ctx.Reflect();
            return false;
        }

        output = foundOutput;
        stack = foundStack;
        return true;
    }

    static bool TryGetOffsetSpaceVectorContexts(
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

    static int ValueAtOrZero(IReadOnlyList<int> values, int index)
        => index < values.Count ? values[index] : 0;

    static void Bury(IFungeExecutionContext ctx)
    {
        var depth = ctx.Pop();
        var value = ctx.Pop();

        if (depth < 0)
        {
            ctx.Push(value);
            for (var i = 0L; i < -(long)depth; i++)
                ctx.Push(0);
            return;
        }

        if (!TryGetStackContext(ctx, out var stack))
            return;

        if (stack.StackDepth < depth)
        {
            ctx.Reflect();
            return;
        }

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        values.Insert(depth, value);
        ReplaceTopStack(ctx, stack.StackDepth, values);
    }

    static void Count(IFungeExecutionContext ctx)
    {
        if (!TryGetStackContext(ctx, out var stack))
            return;

        ctx.Push(stack.StackDepth);
    }

    static void DuplicateTopValues(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        List<int> values = [];
        for (var i = 0; i < count; i++)
            values.Add(ctx.Pop());

        for (var i = values.Count - 1; i >= 0; i--)
        {
            ctx.Push(values[i]);
            ctx.Push(values[i]);
        }
    }

    static void GetFromSpace(IFungeExecutionContext ctx)
    {
        if (!TryGetOffsetSpaceVectorContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (x, y, z) = vector.PopVector();
        var (dx, dy, dz) = vector.PopVector();
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        var (offsetX, offsetY, offsetZ) = offset.StorageOffset;
        var values = new int[count];
        for (var i = 0; i < count; i++)
            values[i] = space.GetCell(x + offsetX + dx * i, y + offsetY + dy * i, z + offsetZ + dz * i);

        for (var i = values.Length - 1; i >= 0; i--)
            ctx.Push(values[i]);
    }

    static void MoveBlockToTop(IFungeExecutionContext ctx)
    {
        var end = ctx.Pop();
        var start = ctx.Pop();
        if (start < 0 || end < 0 || end > start)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetStackContext(ctx, out var stack))
            return;

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        List<int> block = [];
        for (var i = end; i <= start; i++)
            block.Add(ValueAtOrZero(values, i));

        if (end < values.Count)
        {
            var removeCount = Math.Min(start, values.Count - 1) - end + 1;
            values.RemoveRange(end, removeCount);
        }

        var newValues = block.Concat(values).ToList();
        ReplaceTopStack(ctx, stack.StackDepth, newValues);
    }

    static void ReverseTopValues(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        List<int> values = [];
        for (var i = 0; i < count; i++)
            values.Add(ctx.Pop());

        foreach (var value in values)
            ctx.Push(value);
    }

    static void PrintStack(IFungeExecutionContext ctx)
    {
        if (!TryGetOutputStackContext(ctx, out var output, out var stack))
            return;

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            output.WriteString(values[i].ToString(CultureInfo.InvariantCulture));
            output.WriteString(" ");
        }
    }

    static void ReverseAll(IFungeExecutionContext ctx)
    {
        if (!TryGetStackContext(ctx, out var stack))
            return;

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        values.Reverse();
        ReplaceTopStack(ctx, stack.StackDepth, values);
    }

    static void DuplicateSecond(IFungeExecutionContext ctx)
    {
        var top = ctx.Pop();
        var second = ctx.Pop();
        ctx.Push(second);
        ctx.Push(second);
        ctx.Push(top);
    }

    static void SwapSecondAndThird(IFungeExecutionContext ctx)
    {
        var top = ctx.Pop();
        var second = ctx.Pop();
        var third = ctx.Pop();
        ctx.Push(second);
        ctx.Push(third);
        ctx.Push(top);
    }

    static void DropUntilFound(IFungeExecutionContext ctx)
    {
        var target = ctx.Pop();
        if (!TryGetStackContext(ctx, out var stack))
            return;

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        var index = values.IndexOf(target);
        if (index < 0)
        {
            ctx.Reflect();
            return;
        }

        var newValues = values.Skip(index).Prepend(index).ToList();
        ReplaceTopStack(ctx, stack.StackDepth, newValues);
    }

    static void WriteToSpace(IFungeExecutionContext ctx)
    {
        if (!TryGetOffsetSpaceVectorContexts(ctx, out var vector, out var space, out var offset))
            return;

        var (x, y, z) = vector.PopVector();
        var (dx, dy, dz) = vector.PopVector();
        var count = ctx.Pop();
        if (count < 0)
        {
            ctx.Reflect();
            return;
        }

        var (offsetX, offsetY, offsetZ) = offset.StorageOffset;
        for (var i = 0; i < count; i++)
            space.SetCell(x + offsetX + dx * i, y + offsetY + dy * i, z + offsetZ + dz * i, ctx.Pop());
    }

    static void ReverseZeroTerminatedString(IFungeExecutionContext ctx)
    {
        if (!TryGetStackContext(ctx, out var stack))
            return;

        if (stack.StackDepth == 0)
        {
            ctx.Push(0);
            return;
        }

        var values = SnapshotTopStack(ctx, stack.StackDepth);
        if (values[0] != 0)
        {
            ctx.Reflect();
            return;
        }

        var nextTerminator = values.IndexOf(0, 1);
        var segmentEndExclusive = nextTerminator >= 0 ? nextTerminator : values.Count;
        var segment = values.Take(segmentEndExclusive).ToList();
        if (segment.Count == 0)
        {
            ctx.Reflect();
            return;
        }

        var converted = segment.Skip(1).Append(0).Concat(values.Skip(segmentEndExclusive)).ToList();
        ReplaceTopStack(ctx, stack.StackDepth, converted);
    }
}
