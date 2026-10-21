using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
            var mainThread = new MainThread(loop);

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
                return mainThread.Run(() => Thread.CurrentThread.ManagedThreadId == callerThreadId);
            });

            Assert.True(result);
        }

        [Fact]
        public void Run_FromOtherThread_ExecutesOnMainContextThread()
        {
            using var loop = new SingleThreadContext();
            var mainThread = new MainThread(loop);

            var executedOnThreadId = mainThread.Run(() => Thread.CurrentThread.ManagedThreadId);

            Assert.Equal(loop.ThreadId, executedOnThreadId);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, executedOnThreadId);
        }

        /// <summary>
        /// A dedicated thread that pumps this <see cref="SynchronizationContext"/>, like an engine main loop.
        /// </summary>
        sealed class SingleThreadContext : SynchronizationContext, IDisposable
        {
            readonly BlockingCollection<(SendOrPostCallback callback, object? state)> queue = new();
            readonly Thread thread;

            public int ThreadId => thread.ManagedThreadId;

            public SingleThreadContext()
            {
                thread = new Thread(Pump) { IsBackground = true, Name = nameof(SingleThreadContext) };
                thread.Start();
            }

            public override void Post(SendOrPostCallback d, object? state) => queue.Add((d, state));

            public T Invoke<T>(Func<T> func)
            {
                var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                Post(_ =>
                {
                    try { tcs.SetResult(func()); }
                    catch (Exception e) { tcs.SetException(e); }
                }, null);
                if (!tcs.Task.Wait(Timeout))
                    throw new TimeoutException("Main context thread is blocked (deadlock).");
                return tcs.Task.Result;
            }

            void Pump()
            {
                SetSynchronizationContext(this);
                foreach (var (callback, state) in queue.GetConsumingEnumerable())
                    callback(state);
            }

            public void Dispose()
            {
                queue.CompleteAdding();
                // A deadlocked pump never exits: leave its queue alive rather than fault the test host.
                if (thread.Join(Timeout))
                    queue.Dispose();
            }
        }
    }
}
