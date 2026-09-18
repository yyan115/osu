using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using osu.Game.IPC;
using osu.Game.Tests.IPC;
class Program
{
    private static readonly ConcurrentQueue<Exception> unobserved = new();
    private static readonly ConcurrentDictionary<string, int> disposed = new();
    private static int failures;
    static async Task<int> Main()
    {
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            if (e.Exception is ObjectDisposedException ex)
            {
                string key = ex.ObjectName ?? "unknown";
                if (disposed.AddOrUpdate(key, 1, (_, count) => count + 1) == 1)
                    Console.WriteLine($"FIRST DISPOSED {key}: {ex}");
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            unobserved.Enqueue(e.Exception);
            // Record every failure and return a failing process exit code. This is probe-only.
            e.SetObserved();
        };
        foreach (bool serverFirst in new[] { false, true })
        foreach (bool hard in new[] { false, true })
        {
            int before = unobserved.Count;
            for (int i = 0; i < 25; i++)
            {
                try { await RunPair(serverFirst, hard).WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (Exception ex) { failures++; Console.WriteLine($"Direct failure: {ex}"); }
                await Task.Delay(20);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            await Task.Delay(200);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine($"serverFirst={serverFirst} hard={hard}: {unobserved.Count - before} unobserved errors");
        }
        if (unobserved.TryPeek(out var first)) Console.WriteLine($"FIRST UNOBSERVED: {first}");
        foreach (var entry in disposed) Console.WriteLine($"DISPOSED {entry.Key}: {entry.Value}");
        Console.WriteLine($"TOTAL direct={failures} unobserved={unobserved.Count}");
        return failures == 0 && unobserved.IsEmpty ? 0 : 1;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunPair(bool serverFirst, bool hard)
    {
        using var server = new WebSocketServer(54321);
        using var client = new WebSocketClient(54321);
        var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientConnected += _ => accepted.TrySetResult();
        await server.StartAsync();
        await client.Start();
        await accepted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var token = new CancellationToken(hard);
        if (serverFirst)
        {
            await server.StopAsync(token);
            await client.StopAsync();
        }
        else
        {
            await client.StopAsync(token);
            await server.StopAsync();
        }
    }
}
namespace osu.Framework.Logging
{
    public enum LogLevel { Verbose, Important, Error }
    public sealed class Logger
    {
        public static Logger GetLogger(string name) => new();
        public void Add(string text, LogLevel level = LogLevel.Verbose, Exception? exception = null)
        {
            if (level == LogLevel.Error) Console.WriteLine($"{text} {exception}");
        }
    }
}
