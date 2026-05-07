namespace Esolang.Funge.Parser;

/// <summary>
/// Represents a 3D integer vector used for positions and deltas in Funge-98.
/// </summary>
public readonly struct FungeVector : IEquatable<FungeVector>
{
    /// <summary>The X component.</summary>
    public int X { get; }

    /// <summary>The Y component.</summary>
    public int Y { get; }

    /// <summary>The Z component.</summary>
    public int Z { get; }

    /// <summary>Initializes a new <see cref="FungeVector"/> with the given components.</summary>
    public FungeVector(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Initializes a new <see cref="FungeVector"/> in 2D (Z=0).</summary>
    public FungeVector(int x, int y) : this(x, y, 0) { }

    /// <inheritdoc/>
    public bool Equals(FungeVector other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FungeVector v && Equals(v);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(HashCode.Combine(X, Y), Z);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(FungeVector left, FungeVector right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(FungeVector left, FungeVector right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"({X}, {Y}, {Z})";

    /// <summary>Delta for East direction (right): (1, 0).</summary>
    public static readonly FungeVector East = new(1, 0);

    /// <summary>Delta for West direction (left): (-1, 0).</summary>
    public static readonly FungeVector West = new(-1, 0);

    /// <summary>Delta for North direction (up): (0, -1).</summary>
    public static readonly FungeVector North = new(0, -1);

    /// <summary>Delta for South direction (down): (0, 1).</summary>
    public static readonly FungeVector South = new(0, 1);

    /// <summary>Delta for High direction (towards -Z): (0, 0, -1).</summary>
    public static readonly FungeVector High = new(0, 0, -1);

    /// <summary>Delta for Low direction (towards +Z): (0, 0, 1).</summary>
    public static readonly FungeVector Low = new(0, 0, 1);

    /// <summary>Adds two vectors.</summary>
    public static FungeVector operator +(FungeVector a, FungeVector b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Subtracts two vectors.</summary>
    public static FungeVector operator -(FungeVector a, FungeVector b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Negates a vector.</summary>
    public static FungeVector operator -(FungeVector a) => new(-a.X, -a.Y, -a.Z);

    /// <summary>Scales a vector by a scalar.</summary>
    public static FungeVector operator *(FungeVector a, int scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);

    /// <summary>
    /// Rotates 90 degrees clockwise (Turn Right <c>]</c>).
    /// </summary>
    public FungeVector RotateRight() => new(-Y, X, Z);

    /// <summary>
    /// Rotates 90 degrees counter-clockwise (Turn Left <c>[</c>).
    /// </summary>
    public FungeVector RotateLeft() => new(Y, -X, Z);

    /// <summary>
    /// Reflects the vector, reversing direction (<c>r</c>).
    /// </summary>
    public FungeVector Reflect() => new(-X, -Y, -Z);
}
