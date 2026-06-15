using System.Net;
using Serilog;

namespace DigitalBoardGameList.App.Network;

public abstract class TimeThrottledHttp
{
    private const int MaxRetries = 3;
    private const double RateLimitDelayMultiplier = 2.0;
    private static readonly TimeSpan MaxRequestDelay = TimeSpan.FromSeconds(30);

    private RequestDelay _requestDelay;

    protected TimeThrottledHttp(RequestDelay delay)
    {
        _requestDelay = delay;
    }

    protected async Task<string> GetStringAsync(string url)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Log.Debug("[{Type}] Requesting {Url} (attempt {Attempt}/{Max})",
                nameof(TimeThrottledHttp), url, attempt, MaxRetries);

            await _requestDelay.Wait();

            try
            {
                using var response = await SharedHttpClient.Instance.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                var code = response.StatusCode;

                if (code == HttpStatusCode.TooManyRequests)
                {
                    ApplyRateLimitDelay(response, url);
                    continue;
                }

                if (ShouldRetry(code))
                {
                    Log.Warning("[{Type}] HTTP {Code} (attempt: {Attempt}/{Max}) for {Url}",
                        nameof(TimeThrottledHttp), ToStatusCodeStr(code), attempt, MaxRetries, url);

                    continue;
                }

                throw new HttpStatusException(code, url);
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, "[{Type}] Network error (attempt: {Attempt}/{Max}) for {Url}",
                    nameof(TimeThrottledHttp), attempt, MaxRetries, url);
            }
            catch (TaskCanceledException ex)
            {
                Log.Warning(ex, "[{Type}] HTTP request timeout (attempt: {Attempt}/{Max}) for {Url}",
                    nameof(TimeThrottledHttp), attempt, MaxRetries, url);
            }
            finally
            {
                _requestDelay.Restart();
            }
        }

        throw new HttpRequestException($"All {MaxRetries} HTTP request attempts failed for {url}");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout or
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    private void ApplyRateLimitDelay(HttpResponseMessage response, string url)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter != null)
        {
            if (retryAfter.Delta != null)
            {
                var delta = retryAfter.Delta.Value;

                Log.Warning("[{Type}] Rate limited (429). Retry-After delta: {Seconds}s. Url: {Url}",
                    nameof(TimeThrottledHttp), delta.TotalSeconds, url);

                _requestDelay = new RequestDelay(Clamp(delta, MaxRequestDelay));
                return;
            }
            if (retryAfter.Date != null)
            {
                var delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (delta > TimeSpan.Zero)
                {
                    Log.Warning("[{Type}] Rate limited (429). Retry-After date: {Seconds}s. Url: {Url}",
                        nameof(TimeThrottledHttp), delta.TotalSeconds, url);

                    _requestDelay = new RequestDelay(Clamp(delta, MaxRequestDelay));
                    return;
                }

                Log.Warning("[{Type}] Rate limited (429). Retry-After date is invalid. " +
                            "Applying delay multiplier x{Mult}. Url: {Url}",
                    nameof(TimeThrottledHttp), RateLimitDelayMultiplier, url);

                _requestDelay = MultiplyDelay(_requestDelay, RateLimitDelayMultiplier);
                return;
            }
        }

        Log.Warning("[{Type}] Rate limited (429). Retry-After header is not present. " +
                    "Applying delay multiplier x{Mult}. Url: {Url}",
            nameof(TimeThrottledHttp), RateLimitDelayMultiplier, url);

        _requestDelay = MultiplyDelay(_requestDelay, RateLimitDelayMultiplier);
    }

    private static RequestDelay MultiplyDelay(RequestDelay delay, double factor)
    {
        var newMin = Clamp(TimeSpan.FromMilliseconds(delay.MinDelay.TotalMilliseconds * factor), MaxRequestDelay);
        var newMax = Clamp(TimeSpan.FromMilliseconds(delay.MaxDelay.TotalMilliseconds * factor), MaxRequestDelay);
        return new RequestDelay(newMin, newMax);
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan max)
    {
        return value > max ? max : value;
    }

    private static string ToStatusCodeStr(HttpStatusCode statusCode)
    {
        return $"{(int)statusCode} ({statusCode})";
    }

    private class HttpStatusException(HttpStatusCode code, string url)
        : Exception($"Non-retryable HTTP {ToStatusCodeStr(code)} for {url}");
}