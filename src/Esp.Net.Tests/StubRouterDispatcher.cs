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
#endregion

using System;
using System.Collections.Generic;

namespace Esp.Net
{
    public class StubRouterDispatcher : IRouterDispatcher
    {
        private readonly List<Action> _actions = new List<Action>();

        public StubRouterDispatcher()
        {
            HasAccess = true;
        }

        public bool HasAccess { get; set; }

        public bool IsDisposed { get; set; }

        public int QueuedActionCount { get { return _actions.Count; } }

        public bool CheckAccess()
        {
            ThrowIfDisposed();
            return HasAccess;
        }

        public void EnsureAccess()
        {
            ThrowIfDisposed();
            if (!HasAccess) throw new InvalidOperationException("Invalid access");
        }

        public void Dispatch(Action action)
        {
            ThrowIfDisposed();
            _actions.Add(action);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        private void ThrowIfDisposed()
        {
            if(IsDisposed) throw new ObjectDisposedException(string.Empty);
        }

        public void InvokeDispatchedActions(int numberToInvoke)
        {
            var oldHasAccess = HasAccess;
            HasAccess = true;
            for (int i = 0; i < numberToInvoke || i < _actions.Count - 1; i++)
            {
                _actions[i]();
            }
            HasAccess = oldHasAccess;
        }
    }
}