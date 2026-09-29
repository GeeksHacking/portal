using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Aspire.Hosting;

namespace GeeksHackingPortal.Tests.Helpers;

/// <summary>
/// Streams the console output of the API resource started by Aspire so that server-side
/// exceptions can be attached to the test report. Lines are stamped on arrival, which lets a test
/// pull out the lines that were logged while it was running.
/// </summary>
public sealed partial class ServerLogCollector : IAsyncDisposable
{
    private readonly List<(DateTimeOffset At, string Text, bool IsError)> _lines = [];
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<object, DateTimeOffset> _testStarts = new();
    private Task _pump = Task.CompletedTask;

    // Matches default console logger levels ("fail:", "crit:", "warn:") and exception text.
    [GeneratedRegex(@"\b(fail|crit|warn)\b:|exception|\berror\b", RegexOptions.IgnoreCase)]
    private static partial Regex Interesting();

    /// <remarks>
    /// Takes the resource rather than its name: Aspire keys logs by the resource's instance name
    /// (for example <c>api-a1b2c3</c>), and only the <see cref="IResource"/> overload resolves it.
    /// </remarks>
    public void Start(ResourceLoggerService loggers, IResource resource)
    {
        _pump = Task.Run(async () =>
        {
            try
            {
                await foreach (var batch in loggers.WatchAsync(resource).WithCancellation(_cts.Token))
                {
                    var now = DateTimeOffset.UtcNow;
                    lock (_gate)
                    {
                        foreach (var line in batch)
                        {
                            _lines.Add((now, line.Content, line.IsErrorMessage));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
        });
    }

    public void MarkTestStart(object test) => _testStarts[test] = DateTimeOffset.UtcNow;

    /// <summary>
    /// Warnings, errors and exception stack traces logged since <see cref="MarkTestStart"/>.
    /// Concurrent tests share one server, so lines from other tests may appear.
    /// </summary>
    public string GetInterestingSince(object test, int maxLines = 200)
    {
        if (!_testStarts.TryRemove(test, out var start))
        {
            return "";
        }

        var sb = new StringBuilder();
        var count = 0;
        var inTrace = false;
        lock (_gate)
        {
            foreach (var (at, text, isError) in _lines)
            {
                if (at < start)
                {
                    continue;
                }

                var isContinuation = inTrace && (text.StartsWith(' ') || text.StartsWith('\t'));
                inTrace = isError || isContinuation || Interesting().IsMatch(text);
                if (!inTrace)
                {
                    continue;
                }

                if (++count > maxLines)
                {
                    sb.AppendLine("... truncated ...");
                    break;
                }

                sb.AppendLine(text);
            }
        }

        return sb.ToString();
    }

    /// <summary>Writes the complete captured log to a file and returns its path.</summary>
    public string WriteToFile(string path)
    {
        lock (_gate)
        {
            File.WriteAllLines(path, _lines.Select(l => $"{l.At:O} {l.Text}"));
        }

        return path;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            await _pump;
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        _cts.Dispose();
    }
}
