using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class UnityThread
{
    private static SynchronizationContext _context;
    private static int _mainThreadId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        _context = SynchronizationContext.Current;
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static bool IsMainThread =>
        Thread.CurrentThread.ManagedThreadId == _mainThreadId;

    public static Task SwitchToMainThread()
    {
        if (IsMainThread)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        if (_context == null)
        {
            tcs.SetResult(true);
            return tcs.Task;
        }

        _context.Post(_ => tcs.SetResult(true), null);
        return tcs.Task;
    }

    public static void Post(Action action)
    {
        if (action == null) return;

        if (IsMainThread || _context == null)
        {
            action();
            return;
        }

        _context.Post(_ => action(), null);
    }
}