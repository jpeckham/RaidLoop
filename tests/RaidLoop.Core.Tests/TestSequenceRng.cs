using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

internal sealed class TestSequenceRng : IRng
{
    private readonly Queue<int> _sequence;

    public TestSequenceRng(IEnumerable<int> sequence)
    {
        _sequence = new Queue<int>(sequence);
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (_sequence.Count == 0)
        {
            throw new InvalidOperationException("TestSequenceRng exhausted.");
        }

        var offset = _sequence.Dequeue();
        var span = maxExclusive - minInclusive;
        return minInclusive + (offset % span);
    }
}

internal sealed class CyclingRng : IRng
{
    private int _next;

    public int Next(int minInclusive, int maxExclusive)
    {
        var span = maxExclusive - minInclusive;
        var value = _next % span;
        _next++;
        return minInclusive + value;
    }
}
