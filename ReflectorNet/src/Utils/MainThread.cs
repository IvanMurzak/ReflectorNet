using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /*
     * ReflectorNet
     * Author: Ivan Murzak (https://github.com/IvanMurzak)
     * Copyright (c) 2025 Ivan Murzak
     * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
     */

    public class MainThread
    {
        // Declared before Instance: static initializers run in order, and Instance's constructor reads it.
        static readonly SynchronizationContext? typeInitContext = SynchronizationContext.Current;

        public static MainThread Instance { get; set; } = new MainThread();

        readonly SynchronizationContext? mainContext;

        /// <summary>
        /// Uses <see cref="SynchronizationContext.Current"/> of the thread that first touched
        /// <see cref="MainThread"/> (type initialization) as the main context, so subclasses constructed later
        /// on a worker thread still post to the main context.
        /// </summary>
        public MainThread() : this(typeInitContext)
        {
        }

        /// <summary>
        /// Uses <paramref name="mainContext"/> as the main context: work from other threads is posted to it.
        /// <c>null</c> means there is no main context (see <see cref="IsMainThread"/>).
        /// </summary>
        public MainThread(SynchronizationContext? mainContext)
        {
            this.mainContext = mainContext;
        }

        SynchronizationContext MainContext => mainContext ?? throw new InvalidOperationException(
            $"{nameof(MainThread)} has no main {nameof(SynchronizationContext)} to post work to from a non-main thread. " +
            $"Construct it on the main thread, pass the main context explicitly, or install a host-specific {nameof(MainThread)}.");

        /// <summary>
        /// Determines if the current thread is the main thread.
        /// Override for different environments (e.g. Unity).
        /// <para>With a main context, the main thread is the one running on that context: posting to it from
        /// there and blocking on the result would wait forever. Managed thread ids are not a reliable signal,
        /// e.g. the Unity 7 CoreCLR editor on Windows runs its main thread with id 2. Without a main context
        /// it falls back to managed thread id 1.</para>
        /// </summary>
        public virtual bool IsMainThread => mainContext != null
            ? SynchronizationContext.Current == mainContext
            : Thread.CurrentThread.ManagedThreadId == 1;

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
            MainContext.Post(_ =>
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
            MainContext.Post(_ =>
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
            MainContext.Post(_ =>
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
            MainContext.Post(_ =>
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