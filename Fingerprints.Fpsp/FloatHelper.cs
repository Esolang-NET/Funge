namespace Esolang.Funge.Fingerprints.Fpsp;

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
    internal static float Asin(float x) => (float)Math.Asin(x);
    internal static float Acos(float x) => (float)Math.Acos(x);
    internal static float Atan(float x) => (float)Math.Atan(x);
    internal static float Tan(float x) => (float)Math.Tan(x);
    internal static float Sqrt(float x) => (float)Math.Sqrt(x);
    internal static float Log(float x) => (float)Math.Log(x);
    internal static float Log10(float x) => (float)Math.Log10(x);
    internal static float Exp(float x) => (float)Math.Exp(x);
    internal static float Pow(float x, float y) => (float)Math.Pow(x, y);
    internal static float Abs(float x) => Math.Abs(x);
    internal static float Truncate(float x) => (float)Math.Truncate(x);
    internal static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
    internal static float Pi() => (float)Math.PI;
#else
    internal static float Sin(float x) => MathF.Sin(x);
    internal static float Cos(float x) => MathF.Cos(x);
    internal static float Asin(float x) => MathF.Asin(x);
    internal static float Acos(float x) => MathF.Acos(x);
    internal static float Atan(float x) => MathF.Atan(x);
    internal static float Tan(float x) => MathF.Tan(x);
    internal static float Sqrt(float x) => MathF.Sqrt(x);
    internal static float Log(float x) => MathF.Log(x);
    internal static float Log10(float x) => MathF.Log10(x);
    internal static float Exp(float x) => MathF.Exp(x);
    internal static float Pow(float x, float y) => MathF.Pow(x, y);
    internal static float Abs(float x) => MathF.Abs(x);
    internal static float Truncate(float x) => MathF.Truncate(x);
    internal static float Atan2(float y, float x) => MathF.Atan2(y, x);
    internal static float Pi() => MathF.PI;
#endif
}
