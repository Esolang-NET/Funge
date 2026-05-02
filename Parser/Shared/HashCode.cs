#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
// Minimal HashCode polyfill for netstandard2.0
namespace System;

internal static class HashCode
{
    public static int Combine<T1, T2>(T1 v1, T2 v2)
    {
        var h1 = v1?.GetHashCode() ?? 0;
        var h2 = v2?.GetHashCode() ?? 0;
        var rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
    }
}
#endif
