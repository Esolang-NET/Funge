using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.ThreeDsp;

/// <summary>
/// Provides the standard Funge-98 <c>3DSP</c> fingerprint (handprint <c>0x33445350</c>).
/// </summary>
public sealed class ThreeDeeSpaceFingerprint(string? name = null) : IFingerprint
{
    /// <summary>
    /// Gets the default name of the fingerprint, which is <c>"3DSP"</c>.
    /// </summary>
    public const string NAME = "3DSP";

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute(name ?? NAME);

    IReadOnlyDictionary<char, FingerprintInstruction>? _instructions;

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions
        => _instructions ??= new FingerprintBuilder()
            .Add('A', Add)
            .Add('B', Subtract)
            .Add('C', Cross)
            .Add('D', Dot)
            .Add('L', Length)
            .Add('M', ComponentMultiply)
            .Add('N', Normalize)
            .Add('P', CopyMatrix)
            .Add('R', RotationMatrix)
            .Add('S', ScaleMatrix)
            .Add('T', TranslationMatrix)
            .Add('U', Duplicate)
            .Add('V', Perspective)
            .Add('X', TransformVector)
            .Add('Y', MultiplyMatrices)
            .Add('Z', Scale)
            .BuildInstructions();

    static float PopFloat(IFungeExecutionContext ctx)
        => FloatHelper.FromBits(ctx.Pop());

    static void PushFloat(IFungeExecutionContext ctx, float value)
        => ctx.Push(FloatHelper.ToBits(value));

    // Pop vec3: stack is x(bottom), y, z(top). Pop z, y, x.
    static (float X, float Y, float Z) PopVec3(IFungeExecutionContext ctx)
    {
        var fz = PopFloat(ctx);
        var fy = PopFloat(ctx);
        var fx = PopFloat(ctx);
        return (fx, fy, fz);
    }

    // Push vec3: push x, y, z (z on top)
    static void PushVec3(IFungeExecutionContext ctx, float x, float y, float z)
    {
        PushFloat(ctx, x);
        PushFloat(ctx, y);
        PushFloat(ctx, z);
    }

    static bool TryGetSpaceVector(IFungeExecutionContext ctx, out IFungeVectorContext vector, out IFungeSpaceContext space)
    {
        if (ctx is not IFungeVectorContext v || ctx is not IFungeSpaceContext s)
        {
            vector = null!; space = null!;
            ctx.Reflect();
            return false;
        }

        vector = v; space = s;
        return true;
    }

    // Read 4x4 matrix from funge space at (x0,y0,z0): cell at (x0+col, y0+row, z0)
    static float[,] ReadMatrix(IFungeSpaceContext space, int x0, int y0, int z0)
    {
        var m = new float[4, 4];
        for (var row = 0; row < 4; row++)
            for (var col = 0; col < 4; col++)
                m[row, col] = FloatHelper.FromBits(space.GetCell(x0 + col, y0 + row, z0));
        return m;
    }

    // Write 4x4 matrix to funge space at (x0,y0,z0)
    static void WriteMatrix(IFungeSpaceContext space, int x0, int y0, int z0, float[,] m)
    {
        for (var row = 0; row < 4; row++)
            for (var col = 0; col < 4; col++)
                space.SetCell(x0 + col, y0 + row, z0, FloatHelper.ToBits(m[row, col]));
    }

    static void Add(IFungeExecutionContext ctx)
    {
        var (bx, by, bz) = PopVec3(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, ax + bx, ay + by, az + bz);
    }

    static void Subtract(IFungeExecutionContext ctx)
    {
        var (bx, by, bz) = PopVec3(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, ax - bx, ay - by, az - bz);
    }

    static void Cross(IFungeExecutionContext ctx)
    {
        var (bx, by, bz) = PopVec3(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx);
    }

    static void Dot(IFungeExecutionContext ctx)
    {
        var (bx, by, bz) = PopVec3(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushFloat(ctx, ax * bx + ay * by + az * bz);
    }

    static void Length(IFungeExecutionContext ctx)
    {
        var (ax, ay, az) = PopVec3(ctx);
        PushFloat(ctx, FloatHelper.Sqrt(ax * ax + ay * ay + az * az));
    }

    static void ComponentMultiply(IFungeExecutionContext ctx)
    {
        var (bx, by, bz) = PopVec3(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, ax * bx, ay * by, az * bz);
    }

    static void Normalize(IFungeExecutionContext ctx)
    {
        var (ax, ay, az) = PopVec3(ctx);
        var len = FloatHelper.Sqrt(ax * ax + ay * ay + az * az);
        if (len == 0f)
            PushVec3(ctx, 0f, 0f, 0f);
        else
            PushVec3(ctx, ax / len, ay / len, az / len);
    }

    static void Duplicate(IFungeExecutionContext ctx)
    {
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, ax, ay, az);
        PushVec3(ctx, ax, ay, az);
    }

    static void Perspective(IFungeExecutionContext ctx)
    {
        var (ax, ay, az) = PopVec3(ctx);
        var divisor = az == 0f ? 1f : az;
        PushFloat(ctx, ax / divisor);
        PushFloat(ctx, ay / divisor);
    }

    static void Scale(IFungeExecutionContext ctx)
    {
        var n = PopFloat(ctx);
        var (ax, ay, az) = PopVec3(ctx);
        PushVec3(ctx, n * ax, n * ay, n * az);
    }

    static void CopyMatrix(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (dstX, dstY, dstZ) = vector.PopVector();
        var (srcX, srcY, srcZ) = vector.PopVector();
        var m = ReadMatrix(space, srcX, srcY, srcZ);
        WriteMatrix(space, dstX, dstY, dstZ, m);
    }

    static void RotationMatrix(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (dstX, dstY, dstZ) = vector.PopVector();
        var axis = ctx.Pop();
        var angleDeg = PopFloat(ctx);
        var angle = angleDeg * FloatHelper.Pi() / 180f;
        var c = FloatHelper.Cos(angle);
        var s = FloatHelper.Sin(angle);

        var m = new float[4, 4];
        m[3, 3] = 1f;

        switch (axis)
        {
            case 1: // X axis
                m[0, 0] = 1f;
                m[1, 1] = c; m[1, 2] = -s;
                m[2, 1] = s; m[2, 2] = c;
                break;
            case 2: // Y axis
                m[0, 0] = c; m[0, 2] = s;
                m[1, 1] = 1f;
                m[2, 0] = -s; m[2, 2] = c;
                break;
            case 3: // Z axis
                m[0, 0] = c; m[0, 1] = -s;
                m[1, 0] = s; m[1, 1] = c;
                m[2, 2] = 1f;
                break;
            default:
                ctx.Reflect();
                return;
        }

        WriteMatrix(space, dstX, dstY, dstZ, m);
    }

    static void ScaleMatrix(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (dstX, dstY, dstZ) = vector.PopVector();
        var (sx, sy, sz) = PopVec3(ctx);
        var m = new float[4, 4];
        m[0, 0] = sx;
        m[1, 1] = sy;
        m[2, 2] = sz;
        m[3, 3] = 1f;
        WriteMatrix(space, dstX, dstY, dstZ, m);
    }

    static void TranslationMatrix(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (dstX, dstY, dstZ) = vector.PopVector();
        var (ox, oy, oz) = PopVec3(ctx);
        var m = new float[4, 4];
        m[0, 0] = 1f; m[1, 1] = 1f; m[2, 2] = 1f; m[3, 3] = 1f;
        m[0, 3] = ox;
        m[1, 3] = oy;
        m[2, 3] = oz;
        WriteMatrix(space, dstX, dstY, dstZ, m);
    }

    static void TransformVector(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (srcX, srcY, srcZ) = vector.PopVector();
        var (ax, ay, az) = PopVec3(ctx);
        var m = ReadMatrix(space, srcX, srcY, srcZ);

        // Treat a as row vector (1x4) with w=1, multiply by matrix M
        var rx = ax * m[0, 0] + ay * m[1, 0] + az * m[2, 0] + m[3, 0];
        var ry = ax * m[0, 1] + ay * m[1, 1] + az * m[2, 1] + m[3, 1];
        var rz = ax * m[0, 2] + ay * m[1, 2] + az * m[2, 2] + m[3, 2];
        PushVec3(ctx, rx, ry, rz);
    }

    static void MultiplyMatrices(IFungeExecutionContext ctx)
    {
        if (!TryGetSpaceVector(ctx, out var vector, out var space))
            return;

        var (tgtX, tgtY, tgtZ) = vector.PopVector();
        var (sbX, sbY, sbZ) = vector.PopVector();
        var (saX, saY, saZ) = vector.PopVector();

        var a = ReadMatrix(space, saX, saY, saZ);
        var b = ReadMatrix(space, sbX, sbY, sbZ);
        var result = new float[4, 4];

        for (var row = 0; row < 4; row++)
            for (var col = 0; col < 4; col++)
                for (var k = 0; k < 4; k++)
                    result[row, col] += a[row, k] * b[k, col];

        WriteMatrix(space, tgtX, tgtY, tgtZ, result);
    }
}
