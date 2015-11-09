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

namespace Esp.Net
{
    public partial class Router
    {
        private class State
        {
            private readonly ITerminalErrorHandler _terminalErrorHandler;
            private object _modelBeingProcessed;

            public State(ITerminalErrorHandler terminalErrorHandler)
            {
                _terminalErrorHandler = terminalErrorHandler;
                CurrentStatus = Status.Idle;
            }

            public Exception HaltingException { get; private set; }

            public Status CurrentStatus { get; private set; }

            public void MoveToPreProcessing(object modelId)
            {
                _modelBeingProcessed = modelId;
                CurrentStatus = Status.PreEventProcessing;
            }

            public void MoveToEventDispatch()
            {
                CurrentStatus = Status.EventProcessorDispatch;
            }

            public void MoveToPostProcessing()
            {
                CurrentStatus = Status.PostProcessing;
            }

            public void MoveToDispatchModelUpdates()
            {
                _modelBeingProcessed = null;
                CurrentStatus = Status.DispatchModelUpdates;
            }

            public void MoveToHalted(Exception exception)
            {
                HaltingException = exception;
                CurrentStatus = Status.Halted;
            }

            public void MoveToIdle()
            {
                _modelBeingProcessed = null;
                CurrentStatus = Status.Idle;
            }

            public void MoveToExecuting(object modelId)
            {
                var canExecute = 
                    CurrentStatus == Status.EventProcessorDispatch &&
                    _modelBeingProcessed.Equals(modelId);
                if (canExecute)
                {
                    CurrentStatus = Status.Executing;
                }
                else 
                {
                    throw new InvalidOperationException("Can't execute event. You can only execute an event 1) from within the observer passed to IEventObservable.Observe(IEventObserver), 2) when the router is within an existing event loop, 3) when the current event loop is for the same model you are executing against");
                }
            }

            public void EndExecuting()
            {
                if (CurrentStatus != Status.Executing)
                {
                    throw new InvalidOperationException("Can't end executing state as event execution isn't underway.");
                }
                CurrentStatus = Status.EventProcessorDispatch;
            }

            public void ThrowIfHalted()
            {
                if (CurrentStatus == Status.Halted)
                {
                    var error = new Exception("Router halted due to previous error", HaltingException);
                    if (_terminalErrorHandler != null)
                        _terminalErrorHandler.OnError(error);
                    else
                        throw error;
                }
            }
        }
    }
}