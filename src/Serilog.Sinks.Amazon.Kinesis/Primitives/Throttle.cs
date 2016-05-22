// The MIT License(MIT)
// Copyright(c) 2016 Michael Stepura
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Threading;

namespace Primitives
{
    /// <summary>
    /// Throttling of an action.
    /// Features:
    ///     Action is not reentrant.
    ///     Action does not fire after <see cref="Throttle.Dispose"/>.
    ///     <see cref="Throttle.Dispose"/> waits until running action completes.
    /// </summary>
    sealed class Throttle : IDisposable
    {
        private readonly Action _action;
        private readonly TimeSpan _throttlingTime;
        private readonly Timer _timer;

        private int _throttling;

        private const int THROTTLING_FREE = 0;
        private const int THROTTLING_BUSY = 1;
        private const int THROTTLING_TEARDOWN = 2;

        public Throttle(
            Action action,
            TimeSpan throttlingTime
            )
        {
            _action = action;
            _throttlingTime = throttlingTime;
            _timer = new Timer(Callback, null, Timeout.Infinite, Timeout.Infinite);

            _throttling = THROTTLING_FREE;
        }

        private void Callback(object state)
        {
            try
            {
                _action();
            }
            finally
            {
                // ensure throttling flag is reset to THROTTLING_FREE if it was THROTTLING_BUSY, 
                // otherwise it will not run again and Dispose logic will be broken
                Interlocked.CompareExchange(ref _throttling, THROTTLING_FREE, THROTTLING_BUSY);
            }
        }

        /// <summary>
        /// Schedules action execution after throttlingTime delay
        /// if no action is currently scheduled or executed.
        /// </summary>
        /// <returns>
        /// <value>true</value> if action is scheduled to run after throttlingTime delay, <value>false</value> otherwise.
        /// </returns>
        public bool ThrottleAction()
        {
            if (Interlocked.CompareExchange(ref _throttling, THROTTLING_BUSY, THROTTLING_FREE) == THROTTLING_FREE)
            {
                return _timer.Change(_throttlingTime, new TimeSpan(0, 0, 0, 0, Timeout.Infinite));
            }
            return false;
        }

        public void Dispose()
        {
            var state = Interlocked.Exchange(ref _throttling, THROTTLING_TEARDOWN);
            if (state == THROTTLING_BUSY)
            {
                // timer has been scheduled
                // wait until timer reports handler completion 
                using (WaitHandle wh = new ManualResetEvent(false))
                {
                    if (_timer.Dispose(wh))
                    {
                        wh.WaitOne();
                    }
                }
            }
            else if (state == THROTTLING_FREE)
            {
                _timer.Dispose();
            }
            // do not do anything in THROTTLING_TEARDOWN state
            // because another Dispose method might be waiting for handler completion
        }
    }
}
