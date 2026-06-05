namespace Esolang.Funge.Processor;

sealed class FungeRandomSource
{
    readonly object _sync = new();
    Random _random = new();

    public uint NextUInt32(uint exclusiveUpperBound)
    {
        if (exclusiveUpperBound == 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));

        lock (_sync)
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
        lock (_sync)
            return (float)_random.NextDouble();
    }

    public void Reseed(uint seed)
    {
        lock (_sync)
            _random = new Random(unchecked((int)seed));
    }

    public void Reseed()
    {
        lock (_sync)
            _random = new Random();
    }

    uint NextRawUInt32()
    {
        var bytes = new byte[4];
        _random.NextBytes(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}
