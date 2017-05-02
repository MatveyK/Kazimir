using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class AsyncTools
{
    private static Awaiter updateAwaiter;
    private static Awaiter fixedAwaiter;
    private static Awaiter lateUpdateAwaiter;
    private static Awaiter editorUpdateAwaiter;
    private static Awaiter threadPoolAwaiter = new ThreadPoolContextAwaiter();

    public static void WhereAmI(string text)
    {
        if (IsMainThread())
        {
            var contextName = (SynchronizationContext.Current as UnitySynchronizationContext)?.Name ?? "No context";
            Debug.Log($"{text}: main thread, {contextName}, frame: {Time.frameCount}");
        }
        else
        {
            Debug.Log($"{text}: background thread, id: {Thread.CurrentThread.ManagedThreadId}");
        }
    }

    /// <summary>
    /// Returns true if called from the Unity's main thread, and false otherwise.
    /// </summary>
    public static bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == UnityScheduler.MainThreadId;

    /// <summary>
    /// Switches execution to a background thread.
    /// </summary>
    public static Awaiter ToThreadPool() => threadPoolAwaiter;

    /// <summary>
    /// Switches execution to the Update context of the main thread.
    /// </summary>
    [Obsolete("Use ToUpdate(), ToLateUpdate() or ToFixedUpdate() instead.")]
    public static Awaiter ToMainThread() => ToUpdate();

    /// <summary>
	/// Switches execution to the EditorUpdate context of the main thread.
	/// </summary>
	public static Awaiter ToEditorUpdate()
    {
        return editorUpdateAwaiter ?? (editorUpdateAwaiter = new SynchronizationContextAwaiter(UnityScheduler.EditorUpdateScheduler.Context));
    }

    /// <summary>
	/// Switches execution to the Update context of the main thread.
	/// </summary>
	public static Awaiter ToUpdate()
    {
        return updateAwaiter ?? (updateAwaiter = new SynchronizationContextAwaiter(UnityScheduler.UpdateScheduler.Context));
    }

    /// <summary>
	/// Switches execution to the LateUpdate context of the main thread.
	/// </summary>
	public static Awaiter ToLateUpdate()
    {
        return lateUpdateAwaiter ?? (lateUpdateAwaiter = new SynchronizationContextAwaiter(UnityScheduler.LateUpdateScheduler.Context));
    }

    /// <summary>
	/// Switches execution to the FixedUpdate context of the main thread.
	/// </summary>
	public static Awaiter ToFixedUpdate()
    {
        return fixedAwaiter ?? (fixedAwaiter = new SynchronizationContextAwaiter(UnityScheduler.FixedUpdateScheduler.Context));
    }

    /// <summary>
	/// Downloads a file as an array of bytes.
	/// </summary>
	/// <param name="address">File URL</param>
	/// <param name="cancellationToken">Optional cancellation token</param>
	public static Task<byte[]> DownloadAsBytesAsync(string address, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.Factory.StartNew(
            delegate
            {
                using (var webClient = new WebClient())
                {
                    return webClient.DownloadData(address);
                }
            }, cancellationToken);
    }

    /// <summary>
    /// Downloads a file as a string.
    /// </summary>
    /// <param name="address">File URL</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public static Task<string> DownloadAsStringAsync(string address, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.Factory.StartNew(
            delegate
            {
                using (var webClient = new WebClient())
                {
                    return webClient.DownloadString(address);
                }
            }, cancellationToken);
    }

    /// <summary>
    /// Waits for specified number of seconds or until next frame.
    /// 
    /// If the argument is zero or negative, and if called from the main thread from Update or LateUpdate context,
    /// waits until next rendering frame.
    /// 
    /// If the argument is zero or negative, and if called from the main thread from FixedUpdate context,
    /// waits until next physics frame.
    /// </summary>
    /// <param name="seconds">If positive, number of seconds to wait</param>
    public static Awaiter GetAwaiter(this float seconds)
    {
        var context = SynchronizationContext.Current as UnitySynchronizationContext;
        if (seconds <= 0f && context != null)
        {
            return new ContextActivationAwaiter(context);
        }

        return new DelayAwaiter(seconds);
    }

    /// <summary>
    /// Waits for specified number of seconds or until next frame.
    /// 
    /// If the argument is zero or negative, and if called from the main thread from Update or LateUpdate context,
    /// waits until next rendering frame.
    /// 
    /// If the argument is zero or negative, and if called from the main thread from FixedUpdate context,
    /// waits until next physics frame.
    /// </summary>
    /// <param name="seconds">If positive, number of seconds to wait</param>
    public static Awaiter GetAwaiter(this int seconds) => GetAwaiter((float)seconds);

    /// <summary>
    /// Waits until all the tasks are completed.
    /// </summary>
    public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks) => TaskEx.WhenAll(tasks).GetAwaiter();

    /// <summary>
    /// Waits until the process exits.
    /// </summary>
    public static TaskAwaiter<int> GetAwaiter(this Process process)
    {
        var tcs = new TaskCompletionSource<int>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, eventArgs) => tcs.TrySetResult(process.ExitCode);
        if (process.HasExited)
        {
            tcs.TrySetResult(process.ExitCode);
        }
        return tcs.Task.GetAwaiter();
    }

    /// <summary>
    /// Waits for AsyncOperation completion
    /// </summary>
    public static Awaiter GetAwaiter(this AsyncOperation asyncOp) => new AsyncOperationAwaiter(asyncOp);

    #region Different awaiters

    public abstract class Awaiter : INotifyCompletion
    {
        public abstract bool IsCompleted { get; }
        public abstract void OnCompleted(Action action);
        public Awaiter GetAwaiter() => this;
        public void GetResult() { }
    }

    private class DelayAwaiter : Awaiter
    {
        private readonly SynchronizationContext context;
        private readonly float seconds;

        public DelayAwaiter(float seconds)
        {
            context = SynchronizationContext.Current;
            this.seconds = seconds;
        }

        public override bool IsCompleted => (seconds <= 0f);

        public override void OnCompleted(Action action)
        {
            TaskEx.Delay((int)(seconds * 1000)).ContinueWith(prevTask =>
                                                             {
                                                                 if (context != null)
                                                                 {
                                                                     context.Post(state => action(), null);
                                                                 }
                                                                 else
                                                                 {
                                                                     action();
                                                                 }
                                                             });
        }
    }

    private class ContextActivationAwaiter : Awaiter
    {
        private readonly UnitySynchronizationContext context;
        private Action continuation;

        public ContextActivationAwaiter(UnitySynchronizationContext context)
        {
            this.context = context;
        }

        public override bool IsCompleted => false;

        public override void OnCompleted(Action action)
        {
            continuation = action;
            context.Activated += ContextActivationEventHandler;
        }

        private void ContextActivationEventHandler(object sender, EventArgs eventArgs)
        {
            context.Activated -= ContextActivationEventHandler;
            context.Post(state => continuation(), null);
        }
    }

    private class SynchronizationContextAwaiter : Awaiter
    {
        private readonly UnitySynchronizationContext context;

        public SynchronizationContextAwaiter(UnitySynchronizationContext context)
        {
            this.context = context;
        }

        public override bool IsCompleted => context == null || context == SynchronizationContext.Current;

        public override void OnCompleted(Action action) => context.Post(state => action(), null);
    }

    private class ThreadPoolContextAwaiter : Awaiter
    {
        public override bool IsCompleted => IsMainThread() == false;
        public override void OnCompleted(Action action) => ThreadPool.QueueUserWorkItem(state => action(), null);
    }

    private class AsyncOperationAwaiter : Awaiter
    {
        private readonly AsyncOperation asyncOp;
        public AsyncOperationAwaiter(AsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
        }

        public override bool IsCompleted => asyncOp.isDone;
        public override void OnCompleted(Action action)
        {
            Task.Factory.StartNew(async () =>
            {
                while (asyncOp.isDone == false)
                {
                    await 0;
                }
                action();
            },
                CancellationToken.None,
                TaskCreationOptions.None,
                UnityScheduler.UpdateScheduler);
        }
    }

    #endregion
}