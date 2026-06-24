namespace Esolang.Funge.Processor;

sealed class FungeRandomSource : IFungeRandomContext
{
#if NET9_0_OR_GREATER
    readonly Lock _sync = new();
#else
    readonly object _sync = new();
#endif
    Random _random = new();

    public uint NextUInt32(uint exclusiveUpperBound)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfZero(exclusiveUpperBound);
#else
        if (exclusiveUpperBound == 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
#endif    

#if NET9_0_OR_GREATER
        using (_sync.EnterScope())
#else
        lock (_sync)
#endif
        {
            var limit = uint.MaxValue - uint.MaxValue % exclusiveUpperBound;
            while (true)
            {
                var candidate = NextRawUInt32();
                if (candidate < limit)
                    return candidate % exclusiveUpperBound;
            }
        }
    }

    public float NextSingle()
    {
#if NET9_0_OR_GREATER
        using (_sync.EnterScope())
#else
        lock (_sync)
#endif
            return (float)_random.NextDouble();
    }

    public void Reseed(uint seed)
    {
#if NET9_0_OR_GREATER
        using (_sync.EnterScope())
#else
        lock (_sync)
#endif
            _random = new Random(unchecked((int)seed));
    }

    public void Reseed()
    {
#if NET9_0_OR_GREATER
        using (_sync.EnterScope())
#else
        lock (_sync)
#endif
            _random = new Random();
    }

    uint NextRawUInt32()
    {
        var bytes = new byte[4];
        _random.NextBytes(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}
