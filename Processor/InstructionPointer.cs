using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor;

/// <summary>
/// Represents the execution state of a single Instruction Pointer (IP) in Funge-98.
/// </summary>
public sealed class InstructionPointer
{
    /// <summary>Gets the unique identifier for this IP.</summary>
    public int Id { get; }

    /// <summary>Gets or sets the current position in FungeSpace.</summary>
    public FungeVector Position { get; set; }

    /// <summary>Gets or sets the current movement delta (direction).</summary>
    public FungeVector Delta { get; set; } = FungeVector.East;

    /// <summary>Gets or sets the storage offset used by <c>g</c>/<c>p</c> instructions.</summary>
    public FungeVector Offset { get; set; }

    /// <summary>Gets the stack stack for this IP.</summary>
    public StackStack StackStack { get; }

    /// <summary>Gets or sets whether this IP is in string mode.</summary>
    public bool StringMode { get; set; }

    /// <summary>Gets or sets whether this IP has been stopped (by <c>@</c>).</summary>
    public bool IsStopped { get; set; }

    /// <summary>Initializes an IP with the given ID and a new empty stack stack.</summary>
    public InstructionPointer(int id) : this(id, new StackStack()) { }

    private InstructionPointer(int id, StackStack stackStack)
    {
        Id = id;
        StackStack = stackStack;
    }

    /// <summary>
    /// Creates a child IP for the <c>t</c> (Split) instruction.
    /// The child shares the same position, a deep copy of the stack stack,
    /// and a reflected delta.
    /// </summary>
    /// <param name="newId">Unique ID for the new child IP.</param>
    /// <returns>The new child IP.</returns>
    public InstructionPointer CreateChild(int newId) => new(newId, StackStack.Clone())
    {
        Position = Position,
        Delta = Delta.Reflect(),
        Offset = Offset,
        StringMode = StringMode,
    };
}
