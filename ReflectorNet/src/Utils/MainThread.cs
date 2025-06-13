using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public class MainThread
    {
        public static MainThread Instance { get; set; } = new MainThread();

        static SynchronizationContext mainContext = SynchronizationContext.Current;

        /// <summary>
        /// Determines if the current thread is the main thread.
        /// This delegate can be replaced for different environments (e.g. Unity).
        /// </summary>
        public virtual bool IsMainThread => Thread.CurrentThread.ManagedThreadId == 1;

        // Synchronous methods
        public virtual void Run(Task task) => RunAsync(task).Wait();
        public virtual T Run<T>(Task<T> task) => RunAsync(task).Result;
        public virtual T Run<T>(Func<T> func) => RunAsync(func).Result;
        public virtual void Run(Action action) => RunAsync(action).Wait();

        // Asynchronous methods
        public virtual Task RunAsync(Task task)
        {
            if (IsMainThread)
                return task;

            var tcs = new TaskCompletionSource<bool>();
            mainContext.Post(_ =>
            {
                try
                {
                    task.Wait();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        public virtual Task<T> RunAsync<T>(Task<T> task)
        {
            if (IsMainThread)
                return task;

            var tcs = new TaskCompletionSource<T>();
            mainContext.Post(_ =>
            {
                try
                {
                    var result = task.Result;
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public virtual Task<T> RunAsync<T>(Func<T> func)
        {
            if (IsMainThread)
                return Task.FromResult(func());

            var tcs = new TaskCompletionSource<T>();
            mainContext.Post(_ =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public virtual Task RunAsync(Action action)
        {
            if (IsMainThread)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            mainContext.Post(_ =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }
    }
}