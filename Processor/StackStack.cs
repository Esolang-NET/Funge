namespace Esolang.Funge.Processor;

/// <summary>
/// Implements the Funge-98 stack stack: a stack of stacks.
/// The topmost stack is the TOSS (Top Of Stack Stack).
/// The second stack (if present) is the SOSS (Second On Stack Stack).
/// </summary>
public sealed class StackStack
{
    private readonly LinkedList<Stack<int>> _stacks = new();

    /// <summary>Initializes a new stack stack with a single empty TOSS.</summary>
    public StackStack() => _stacks.AddFirst(new Stack<int>());

    private StackStack(LinkedList<Stack<int>> stacks) => _stacks = stacks;

    /// <summary>Gets the top-of-stack-stack (current active stack).</summary>
    public Stack<int> TOSS => _stacks.First!.Value;

    /// <summary>Gets the second-on-stack-stack, or <see langword="null"/> if there is only one stack.</summary>
    public Stack<int>? SOSS => _stacks.Count >= 2 ? _stacks.First!.Next!.Value : null;

    /// <summary>Gets whether a SOSS exists.</summary>
    public bool HasSOSS => _stacks.Count >= 2;

    /// <summary>Gets the total number of stacks in the stack stack.</summary>
    public int StackCount => _stacks.Count;

    /// <summary>Pushes a value onto the TOSS.</summary>
    public void Push(int value) => TOSS.Push(value);

    /// <summary>Pops a value from the TOSS. Returns <c>0</c> if the TOSS is empty.</summary>
    public int Pop() => TOSS.Count > 0 ? TOSS.Pop() : 0;

    /// <summary>Peeks at the top of the TOSS without removing it. Returns <c>0</c> if empty.</summary>
    public int Peek() => TOSS.Count > 0 ? TOSS.Peek() : 0;

    /// <summary>Pushes a new empty stack onto the stack stack, making it the new TOSS.</summary>
    public void PushNewStack() => _stacks.AddFirst(new Stack<int>());

    /// <summary>
    /// Pops the current TOSS from the stack stack (discarding its remaining contents),
    /// making the previous SOSS the new TOSS. Does nothing if there is only one stack.
    /// </summary>
    public void PopCurrentStack()
    {
        if (_stacks.Count > 1)
            _stacks.RemoveFirst();
    }

    /// <summary>Clears all items from the TOSS.</summary>
    public void ClearToss() => TOSS.Clear();

    /// <summary>Enumerates all stacks from TOSS downward.</summary>
    public IEnumerable<Stack<int>> AllStacks => _stacks;

    /// <summary>Creates a deep copy of this stack stack.</summary>
    public StackStack Clone()
    {
        var newList = new LinkedList<Stack<int>>();
        foreach (var stack in _stacks)
            newList.AddLast(new Stack<int>(stack.Reverse()));
        return new StackStack(newList);
    }
}
