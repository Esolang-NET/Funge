namespace Esolang.Funge.Fingerprints.Ical;

/// <summary>
/// Provides the standard Funge-98 <c>ICAL</c> fingerprint (handprint <c>0x4943414C</c>).
/// </summary>
public sealed class IntercalFingerprint : IFingerprint, IFungeInstructionPointerLifecycle
{
    readonly Dictionary<int, Stack<(int X, int Y, int Z)>> _addressStacks = [];

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("ICAL");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="IntercalFingerprint"/>.</summary>
    public IntercalFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', UnaryAnd)
            .Add('F', Forget)
            .Add('I', Mingle)
            .Add('N', Next)
            .Add('O', UnaryOr)
            .Add('R', Resume)
            .Add('S', Select)
            .Add('X', UnaryXor)
            .BuildInstructions();

    static int DetermineWidth(int value)
    {
        var u = unchecked((uint)value);
        if (u <= 0xFFFF) return 16;
        if (u <= 0xFFFFFFFF) return 32;
        return 64;
    }

    static int RotateRight(int value, int bitWidth)
    {
        var u = unchecked((uint)value);
        return bitWidth == 16
            ? unchecked((int)(ushort)(u >> 1 & 0x7FFFu | (u & 1u) << 15))
            : unchecked((int)(u >> 1 | (u & 1u) << 31));
    }

    static void UnaryAnd(IFungeExecutionContext ctx)
    {
        var a = ctx.Pop();
        var w = DetermineWidth(a);
        ctx.Push(a & RotateRight(a, w));
    }

    static void UnaryOr(IFungeExecutionContext ctx)
    {
        var a = ctx.Pop();
        var w = DetermineWidth(a);
        ctx.Push(a | RotateRight(a, w));
    }

    static void UnaryXor(IFungeExecutionContext ctx)
    {
        var a = ctx.Pop();
        var w = DetermineWidth(a);
        ctx.Push(a ^ RotateRight(a, w));
    }

    static void Mingle(IFungeExecutionContext ctx)
    {
        // Mingle(a, b): result bit 2k+1 = a bit k, bit 2k = b bit k
        var b = ctx.Pop();
        var a = ctx.Pop();
        var result = 0;
        for (var k = 0; k < 16; k++)
        {
            if ((a & (1 << k)) != 0)
                result |= 1 << (2 * k + 1);
            if ((b & (1 << k)) != 0)
                result |= 1 << (2 * k);
        }

        ctx.Push(result);
    }

    static void Select(IFungeExecutionContext ctx)
    {
        // Select(a, b): collect bits of a where b's bit is set, right-justified
        var b = ctx.Pop();
        var a = ctx.Pop();
        var result = 0;
        var pos = 0;
        for (var k = 0; k < 32; k++)
        {
            if ((b & (1 << k)) != 0)
            {
                if ((a & (1 << k)) != 0)
                    result |= 1 << pos;
                pos++;
            }
        }

        ctx.Push(result);
    }

    Stack<(int X, int Y, int Z)> GetStack(int id)
    {
        if (!_addressStacks.TryGetValue(id, out var stack))
        {
            stack = new Stack<(int X, int Y, int Z)>();
            _addressStacks[id] = stack;
        }

        return stack;
    }

    void Next(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos || ctx is not IFungeVectorContext vector)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetId(ctx, out var id))
            return;

        var stack = GetStack(id);
        if (stack.Count >= 79)
        {
            ctx.Reflect();
            return;
        }

        var target = vector.PopVector();
        stack.Push(pos.Position);
        pos.Position = target;
    }

    void Resume(IFungeExecutionContext ctx)
    {
        if (ctx is not IFungePositionContext pos)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetId(ctx, out var id))
            return;

        var n = ctx.Pop();
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        var stack = GetStack(id);
        var last = pos.Position;
        var popped = 0;
        while (popped < n && stack.Count > 0)
        {
            last = stack.Pop();
            popped++;
        }

        if (popped > 0)
            pos.Position = last;
    }

    void Forget(IFungeExecutionContext ctx)
    {
        if (!TryGetId(ctx, out var id))
            return;

        var n = ctx.Pop();
        if (n < 0)
        {
            ctx.Reflect();
            return;
        }

        var stack = GetStack(id);
        for (var i = 0; i < n && stack.Count > 0; i++)
            stack.Pop();
    }

    static bool TryGetId(IFungeExecutionContext ctx, out int id)
    {
        if (ctx is not IFungeInstructionPointerContext ip)
        {
            id = 0;
            ctx.Reflect();
            return false;
        }

        id = ip.InstructionPointerId;
        return true;
    }

    /// <inheritdoc/>
    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
    {
        if (_addressStacks.TryGetValue(parentInstructionPointerId, out var parentStack))
        {
            var childStack = new Stack<(int X, int Y, int Z)>(new Stack<(int X, int Y, int Z)>(parentStack));
            _addressStacks[childInstructionPointerId] = childStack;
        }
    }

    /// <inheritdoc/>
    public void OnInstructionPointerTerminated(int instructionPointerId)
        => _addressStacks.Remove(instructionPointerId);
}
