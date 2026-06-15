using System.Diagnostics;
using Serilog;

namespace DigitalBoardGameList.App.Network;

public class RequestDelay
{
    private const int MinRemainingDelayMs = 20;

    public TimeSpan MinDelay { get; }
    public TimeSpan MaxDelay { get; }

    private readonly Stopwatch _sw = new();

    public RequestDelay(TimeSpan delay)
    {
        MinDelay = delay;
        MaxDelay = delay;
    }

    public RequestDelay(TimeSpan min, TimeSpan max)
    {
        MinDelay = min;
        MaxDelay = max;
    }

    public static RequestDelay FromSeconds(double min, double max)
    {
        return new RequestDelay(TimeSpan.FromSeconds(min), TimeSpan.FromSeconds(max));
    }

    public static RequestDelay FromSeconds(double delay)
    {
        return new RequestDelay(TimeSpan.FromSeconds(delay));
    }

    public void Restart()
    {
        _sw.Restart();
    }

    public async Task Wait()
    {
        if (!_sw.IsRunning)
            return;

        int delayMs;

        if (MinDelay == MaxDelay)
        {
            delayMs = (int)MinDelay.TotalMilliseconds;
        }
        else
        {
            var min = (int)MinDelay.TotalMilliseconds;
            var max = (int)MaxDelay.TotalMilliseconds;
            delayMs = Random.Shared.Next(min, max);
        }

        var finalDelay = delayMs - (int)_sw.ElapsedMilliseconds;

        if (finalDelay > MinRemainingDelayMs)
        {
            Log.Debug("[{Type}] Waiting {Delay} ms", nameof(RequestDelay), finalDelay);
            await Task.Delay(finalDelay);
        }
    }
}