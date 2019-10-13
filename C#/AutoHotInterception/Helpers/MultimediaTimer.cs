using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoHotInterception.Helpers
{
    public class MultimediaTimer : IDisposable
    {
        private const int EventTypeSingle = 0;
        private const int EventTypePeriodic = 1;

        private static readonly Task TaskDone = Task.FromResult<object>(null);

        private bool disposed = false;
        private int interval, resolution;
        private volatile uint timerId;

        // Hold the timer callback to prevent garbage collection.
        private readonly MultimediaTimerCallback Callback;

        public MultimediaTimer()
        {
            Callback = new MultimediaTimerCallback(TimerCallbackMethod);
            Resolution = 5;
            Interval = 10;
        }

        ~MultimediaTimer()
        {
            Dispose(false);
        }

        /// <summary>
        /// The period of the timer in milliseconds.
        /// </summary>
        public int Interval
        {
            get
            {
                return interval;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                interval = value;
                if (Resolution > Interval)
                    Resolution = value;
            }
        }

        /// <summary>
        /// The resolution of the timer in milliseconds. The minimum resolution is 0, meaning highest possible resolution.
        /// </summary>
        public int Resolution
        {
            get
            {
                return resolution;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                resolution = value;
            }
        }

        /// <summary>
        /// Gets whether the timer has been started yet.
        /// </summary>
        public bool IsRunning
        {
            get { return timerId != 0; }
        }

        public static Task Delay(int millisecondsDelay, CancellationToken token = default(CancellationToken))
        {
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay", millisecondsDelay, "The value cannot be less than 0.");
            }

            if (millisecondsDelay == 0)
            {
                return TaskDone;
            }

            token.ThrowIfCancellationRequested();

            // allocate an object to hold the callback in the async state.
            object[] state = new object[1];
            var completionSource = new TaskCompletionSource<object>(state);
            MultimediaTimerCallback callback = (uint id, uint msg, ref uint uCtx, uint rsv1, uint rsv2) =>
            {
                // Note we don't need to kill the timer for one-off events.
                completionSource.TrySetResult(null);
            };

            state[0] = callback;
            UInt32 userCtx = 0;
            var timerId = NativeMethods.TimeSetEvent((uint)millisecondsDelay, (uint)0, callback, ref userCtx, EventTypeSingle);
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return completionSource.Task;
        }

        public void Start()
        {
            CheckDisposed();

            if (IsRunning)
                throw new InvalidOperationException("Timer is already running");

            // Event type = 0, one off event
            // Event type = 1, periodic event
            UInt32 userCtx = 0;
            timerId = NativeMethods.TimeSetEvent((uint)Interval, (uint)Resolution, Callback, ref userCtx, EventTypePeriodic);
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        public void Stop()
        {
            CheckDisposed();

            if (!IsRunning)
                throw new InvalidOperationException("Timer has not been started");

            StopInternal();
        }

        private void StopInternal()
        {
            NativeMethods.TimeKillEvent(timerId);
            timerId = 0;
        }

        public event EventHandler Elapsed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void TimerCallbackMethod(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2)
        {
            var handler = Elapsed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("MultimediaTimer");
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
            if (IsRunning)
            {
                StopInternal();
            }

            if (disposing)
            {
                Elapsed = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    internal delegate void MultimediaTimerCallback(UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2);

    internal static class NativeMethods
    {
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        internal static extern UInt32 TimeSetEvent(UInt32 msDelay, UInt32 msResolution, MultimediaTimerCallback callback, ref UInt32 userCtx, UInt32 eventType);

        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        internal static extern void TimeKillEvent(UInt32 uTimerId);
    }
}
