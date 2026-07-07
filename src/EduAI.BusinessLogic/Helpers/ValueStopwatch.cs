using System.Diagnostics;

namespace EduAI.BusinessLogic.Helpers;

/// <summary>
/// A lightweight stopwatch that avoids allocating a <see cref="Stopwatch"/> on every operation.
/// Use <c>var sw = ValueStopwatch.StartNew();</c> and later <c>sw.GetElapsedMs()</c>.
/// </summary>
public readonly struct ValueStopwatch
{
    private static readonly double TimestampToMs = 1000.0 / Stopwatch.Frequency;

    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    public double GetElapsedMs()
    {
        var elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
        return elapsed * TimestampToMs;
    }
}
