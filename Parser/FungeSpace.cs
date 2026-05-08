namespace Esolang.Funge.Parser;

/// <summary>
/// Represents the Funge-98 program space: a sparse, conceptually infinite 3D grid of integer cells.
/// Unset cells default to the space character (ASCII 32).
/// </summary>
public sealed class FungeSpace
{
    private readonly Dictionary<FungeVector, int> _cells = new();
    private int _minX, _minY, _minZ, _maxX, _maxY, _maxZ;
    private bool _hasAny;

    private void IncludeInBounds(FungeVector pos)
    {
        if (!_hasAny)
        {
            _minX = _maxX = pos.X;
            _minY = _maxY = pos.Y;
            _minZ = _maxZ = pos.Z;
            _hasAny = true;
            return;
        }

        if (pos.X < _minX) _minX = pos.X;
        if (pos.X > _maxX) _maxX = pos.X;
        if (pos.Y < _minY) _minY = pos.Y;
        if (pos.Y > _maxY) _maxY = pos.Y;
        if (pos.Z < _minZ) _minZ = pos.Z;
        if (pos.Z > _maxZ) _maxZ = pos.Z;
    }

    /// <summary>
    /// Gets or sets the integer value at the given position.
    /// Unset positions return <c>' '</c> (32).
    /// Setting a cell to <c>' '</c> removes it from the space.
    /// </summary>
    public int this[FungeVector pos]
    {
        get => _cells.TryGetValue(pos, out var v) ? v : ' ';
        set
        {
            if (value == ' ')
            {
                _cells.Remove(pos);
            }
            else
            {
                _cells[pos] = value;
                IncludeInBounds(pos);
            }
        }
    }

    /// <summary>
    /// Ensures the given position is included in the Least Significant Bounding Box
    /// even when the cell value is a space and therefore not explicitly stored.
    /// </summary>
    public void EnsureBounds(FungeVector pos) => IncludeInBounds(pos);

    /// <summary>Minimum X coordinate of the populated bounding box.</summary>
    public int MinX => _minX;

    /// <summary>Minimum Y coordinate of the populated bounding box.</summary>
    public int MinY => _minY;

    /// <summary>Maximum X coordinate of the populated bounding box.</summary>
    public int MaxX => _maxX;

    /// <summary>Maximum Y coordinate of the populated bounding box.</summary>
    public int MaxY => _maxY;

    /// <summary>Minimum Z coordinate of the populated bounding box.</summary>
    public int MinZ => _minZ;

    /// <summary>Maximum Z coordinate of the populated bounding box.</summary>
    public int MaxZ => _maxZ;

    /// <summary>
    /// Advances a position by <paramref name="delta"/>, wrapping around the Least Significant
    /// Bounding Box (LSAB) when the result would leave it.
    /// </summary>
    /// <param name="pos">Current position.</param>
    /// <param name="delta">Movement delta.</param>
    /// <returns>The next position after wrapping.</returns>
    public FungeVector Advance(FungeVector pos, FungeVector delta)
    {
        if (!_hasAny)
            return pos;

        var nextX = pos.X + delta.X;
        var nextY = pos.Y + delta.Y;
        var nextZ = pos.Z + delta.Z;

        var width = _maxX - _minX + 1;
        var height = _maxY - _minY + 1;
        var depth = _maxZ - _minZ + 1;

        if (nextX < _minX)
            nextX = _maxX - ((_minX - nextX - 1) % width);
        else if (nextX > _maxX)
            nextX = _minX + ((nextX - _maxX - 1) % width);

        if (nextY < _minY)
            nextY = _maxY - ((_minY - nextY - 1) % height);
        else if (nextY > _maxY)
            nextY = _minY + ((nextY - _maxY - 1) % height);

        if (nextZ < _minZ)
            nextZ = _maxZ - ((_minZ - nextZ - 1) % depth);
        else if (nextZ > _maxZ)
            nextZ = _minZ + ((nextZ - _maxZ - 1) % depth);

        return new FungeVector(nextX, nextY, nextZ);
    }
}
