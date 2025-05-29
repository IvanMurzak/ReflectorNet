using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class MainThread
    {
        static SynchronizationContext mainContext = SynchronizationContext.Current;

        /// <summary>
        /// Determines if the current thread is the main thread.
        /// This delegate can be replaced for different environments (e.g. Unity).
        /// </summary>
        public static Func<bool> IsMainThread { get; set; } = () => Thread.CurrentThread.ManagedThreadId == 1;

        /// <summary>
        /// Pushes a task to be executed on the main thread.
        /// This delegate can be replaced for different environments (e.g. Unity).
        /// </summary>
        public static Func<Task, Task> PushToMainThread { get; set; } = (task) =>
        {
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
        };

        // Synchronous methods
        public static void Run(Task task) => RunAsync(task).Wait();
        public static T Run<T>(Task<T> task) => RunAsync(task).Result;
        public static T Run<T>(Func<T> func) => RunAsync(func).Result;
        public static void Run(Action action) => RunAsync(action).Wait();

        // Asynchronous methods
        public static Task RunAsync(Task task)
        {
            if (IsMainThread())
                return task;

            return PushToMainThread(task);
        }

        public static Task<T> RunAsync<T>(Task<T> task)
        {
            if (IsMainThread())
                return task;

            var taskResult = PushToMainThread(task).ContinueWith<T>(t =>
            {
                if (t.IsFaulted)
                    throw t.Exception.InnerException;
                return task.Result;
            });

            taskResult.Start();
            return taskResult;
        }

        public static Task<T> RunAsync<T>(Func<T> func)
        {
            if (IsMainThread())
                return Task.FromResult(func());

            return RunAsync(new Task<T>(func));
        }

        public static Task RunAsync(Action action)
        {
            if (IsMainThread())
            {
                action();
                return Task.CompletedTask;
            }

            return RunAsync(new Task(action));
        }
    }
}