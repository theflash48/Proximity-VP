using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public static class UnityThread
{
    private static SynchronizationContext _context;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        _context = SynchronizationContext.Current;
    }

    public static Task SwitchToMainThread()
    {
        var tcs = new TaskCompletionSource<bool>();
        _context.Post(_ => tcs.SetResult(true), null);
        return tcs.Task;
    }
}
