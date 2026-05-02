namespace Esolang.Funge.Parser;

/// <summary>
/// Represents a 2D integer vector used for positions and deltas in Funge-98.
/// </summary>
public readonly struct FungeVector : IEquatable<FungeVector>
{
    /// <summary>The X component.</summary>
    public int X { get; }

    /// <summary>The Y component.</summary>
    public int Y { get; }

    /// <summary>Initializes a new <see cref="FungeVector"/> with the given components.</summary>
    public FungeVector(int x, int y) { X = x; Y = y; }

    /// <inheritdoc/>
    public bool Equals(FungeVector other) => X == other.X && Y == other.Y;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FungeVector v && Equals(v);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(FungeVector left, FungeVector right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(FungeVector left, FungeVector right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"({X}, {Y})";

    /// <summary>Delta for East direction (right): (1, 0).</summary>
    public static readonly FungeVector East = new FungeVector(1, 0);

    /// <summary>Delta for West direction (left): (-1, 0).</summary>
    public static readonly FungeVector West = new FungeVector(-1, 0);

    /// <summary>Delta for North direction (up): (0, -1).</summary>
    public static readonly FungeVector North = new FungeVector(0, -1);

    /// <summary>Delta for South direction (down): (0, 1).</summary>
    public static readonly FungeVector South = new FungeVector(0, 1);

    /// <summary>Adds two vectors.</summary>
    public static FungeVector operator +(FungeVector a, FungeVector b) => new FungeVector(a.X + b.X, a.Y + b.Y);

    /// <summary>Subtracts two vectors.</summary>
    public static FungeVector operator -(FungeVector a, FungeVector b) => new FungeVector(a.X - b.X, a.Y - b.Y);

    /// <summary>Negates a vector.</summary>
    public static FungeVector operator -(FungeVector a) => new FungeVector(-a.X, -a.Y);

    /// <summary>Scales a vector by a scalar.</summary>
    public static FungeVector operator *(FungeVector a, int scalar) => new FungeVector(a.X * scalar, a.Y * scalar);

    /// <summary>
    /// Rotates 90 degrees clockwise (Turn Right <c>]</c>).
    /// </summary>
    public FungeVector RotateRight() => new FungeVector(-Y, X);

    /// <summary>
    /// Rotates 90 degrees counter-clockwise (Turn Left <c>[</c>).
    /// </summary>
    public FungeVector RotateLeft() => new FungeVector(Y, -X);

    /// <summary>
    /// Reflects the vector, reversing direction (<c>r</c>).
    /// </summary>
    public FungeVector Reflect() => new FungeVector(-X, -Y);
}
