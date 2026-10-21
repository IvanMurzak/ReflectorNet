using System;
using System.Collections.Concurrent;
using System.Threading;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class MainThreadTests : BaseTest
    {
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public MainThreadTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void IsMainThread_OnMainContextThread_IsTrueRegardlessOfManagedThreadId()
        {
            using var loop = new SingleThreadContext();

            var result = loop.Invoke(() =>
            {
                var mainThread = new MainThread(SynchronizationContext.Current!);
                return (mainThread.IsMainThread, Thread.CurrentThread.ManagedThreadId);
            });

            _output.WriteLine($"Main context thread id: {result.ManagedThreadId}");
            Assert.NotEqual(1, result.ManagedThreadId);
            Assert.True(result.IsMainThread);
        }

        [Fact]
        public void IsMainThread_OnOtherThread_IsFalse()
        {
            using var loop = new SingleThreadContext();
            var mainThread = new MainThread(loop.Context);

            Assert.False(mainThread.IsMainThread);
        }

        [Fact]
        public void Run_OnMainContextThread_ExecutesInlineWithoutDeadlock()
        {
            using var loop = new SingleThreadContext();

            var result = loop.Invoke(() =>
            {
                var mainThread = new MainThread(SynchronizationContext.Current!);
                var callerThreadId = Thread.CurrentThread.ManagedThreadId;
                // Before the fix, a main thread with id != 1 posted this to its own context and blocked on it.
                return mainThread.Run(() => Thread.CurrentThread.ManagedThreadId == callerThreadId);
            });

            Assert.True(result);
        }

        [Fact]
        public void Run_FromOtherThread_ExecutesOnMainContextThread()
        {
            using var loop = new SingleThreadContext();
            var mainThread = new MainThread(loop.Context);

            var executedOnThreadId = mainThread.Run(() => Thread.CurrentThread.ManagedThreadId);

            Assert.Equal(loop.ThreadId, executedOnThreadId);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, executedOnThreadId);
        }

        /// <summary>
        /// A dedicated thread that pumps its own <see cref="SynchronizationContext"/>, like an engine main loop.
        /// </summary>
        sealed class SingleThreadContext : IDisposable
        {
            readonly BlockingCollection<(SendOrPostCallback callback, object? state)> queue = new();
            readonly Thread thread;
            readonly ManualResetEventSlim started = new();

            public SynchronizationContext Context { get; }
            public int ThreadId { get; private set; }

            public SingleThreadContext()
            {
                Context = new QueueContext(queue);
                thread = new Thread(Pump) { IsBackground = true, Name = nameof(SingleThreadContext) };
                thread.Start();
                if (!started.Wait(Timeout))
                    throw new TimeoutException("Main context thread did not start.");
            }

            public T Invoke<T>(Func<T> func)
            {
                var done = new ManualResetEventSlim();
                T result = default!;
                Exception? error = null;
                Context.Post(_ =>
                {
                    try { result = func(); }
                    catch (Exception e) { error = e; }
                    finally { done.Set(); }
                }, null);
                if (!done.Wait(Timeout))
                    throw new TimeoutException("Main context thread is blocked (deadlock).");
                if (error != null)
                    throw new InvalidOperationException("Callback on the main context thread failed.", error);
                return result;
            }

            void Pump()
            {
                SynchronizationContext.SetSynchronizationContext(Context);
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                started.Set();
                foreach (var (callback, state) in queue.GetConsumingEnumerable())
                    callback(state);
            }

            public void Dispose() => queue.CompleteAdding();

            sealed class QueueContext : SynchronizationContext
            {
                readonly BlockingCollection<(SendOrPostCallback callback, object? state)> queue;

                public QueueContext(BlockingCollection<(SendOrPostCallback callback, object? state)> queue)
                {
                    this.queue = queue;
                }

                public override void Post(SendOrPostCallback d, object? state) => queue.Add((d, state));
            }
        }
    }
}
