namespace Esolang.Funge.Fingerprints.ThreeDsp;

/// <summary>
/// Cross-framework helper for single-precision float bit conversions and math operations.
/// Provides polyfills for APIs not available in netstandard2.0.
/// </summary>
static class FloatHelper
{
    internal static float FromBits(int bits)
    {
#if NETSTANDARD2_0
        var bytes = BitConverter.GetBytes(bits);
        return BitConverter.ToSingle(bytes, 0);
#else
        return BitConverter.Int32BitsToSingle(bits);
#endif
    }

    internal static int ToBits(float value)
    {
#if NETSTANDARD2_0
        var bytes = BitConverter.GetBytes(value);
        return BitConverter.ToInt32(bytes, 0);
#else
        return BitConverter.SingleToInt32Bits(value);
#endif
    }

#if NETSTANDARD2_0
    internal static float Sin(float x) => (float)Math.Sin(x);
    internal static float Cos(float x) => (float)Math.Cos(x);
    internal static float Sqrt(float x) => (float)Math.Sqrt(x);
    internal static float Pi() => (float)Math.PI;
#else
    internal static float Sin(float x) => MathF.Sin(x);
    internal static float Cos(float x) => MathF.Cos(x);
    internal static float Sqrt(float x) => MathF.Sqrt(x);
    internal static float Pi() => MathF.PI;
#endif
}
