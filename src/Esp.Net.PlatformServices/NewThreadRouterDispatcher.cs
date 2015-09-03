#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// Note NewThreadRouterDispatcher is based on EventLoopScheduler from rx. However it is much cut down and notion of time has been removed.
// That license is here https://github.com/Reactive-Extensions/Rx.NET/blob/master/Rx.NET/Source/license.txt
// also copied below: 
//
// Copyright(c) Microsoft Open Technologies, Inc.All rights reserved.
// Microsoft Open Technologies would like to thank its contributors, a list
// of whom are at http://rx.codeplex.com/wikipage?title=Contributors.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.  
#endregion

using System;
using System.Collections.Generic;
using System.Threading;

namespace Esp.Net
{
    public class NewThreadRouterDispatcher : IRouterDispatcher
    {
        private static int _threadNameCounter;
        private readonly Func<ThreadStart, Thread> _threadFactory;
        private readonly object _gate = new object();
        private readonly SemaphoreSlim _evt = new SemaphoreSlim(0);
        private readonly Queue<Action> _actionDispatchQueue = new Queue<Action>();
        private Thread _thread;
        private bool _disposed;

        public NewThreadRouterDispatcher()
            : this(a => new Thread(a) { Name = "Router Dispatcher " + Interlocked.Increment(ref _threadNameCounter), IsBackground = true })
        {
        }

        public NewThreadRouterDispatcher(Func<ThreadStart, Thread> threadFactory)
        {
            _threadFactory = threadFactory;
        }

        public bool CheckAccess()
        {
            lock (_gate)
            {
                EnsureThread();
                return Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId;
            }
        }

        public void EnsureAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Router accessed on invalid thread");
            }
        }

        public void Dispatch(Action action)
        {
            if (action == null) throw new ArgumentNullException("action");

            lock (_gate)
            {
                if (_disposed) throw new ObjectDisposedException(string.Empty);

                _actionDispatchQueue.Enqueue(action);
                _evt.Release();

                EnsureThread();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _evt.Release();
                }
            }
        }

        private void Run()
        {
            while (true)
            {
                _evt.Wait();

                var readyActions = default(Action[]);

                lock (_gate)
                {
                    //
                    // Bug fix that ensures the number of calls to Release never greatly exceeds the number of calls to Wait.
                    // See work item #37: https://rx.codeplex.com/workitem/37
                    //
                    while (_evt.CurrentCount > 0) _evt.Wait();

                    //
                    // The event could have been set by a call to Dispose. This takes priority over anything else. We quit the
                    // loop immediately. Subsequent calls to Schedule won't ever create a new thread.
                    //
                    if (_disposed)
                    {
                        _evt.Dispose();
                        return;
                    }

                    if (_actionDispatchQueue.Count > 0)
                    {
                        readyActions = _actionDispatchQueue.ToArray();
                        _actionDispatchQueue.Clear();
                    }
                }

                if (readyActions != null)
                {
                    foreach (var item in readyActions)
                    {
                        item.Invoke();
                    }
                }
            }
        }

        private void EnsureThread()
        {
            if (_thread == null)
            {
                _thread = _threadFactory(Run);
                _thread.Start();
            }
        }
    }
}