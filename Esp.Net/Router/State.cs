using System;

namespace Esp.Net.Router
{
    internal class State
    {
        public State()
        {
            CurrentStatus = Status.Idle;
        }

        public Exception HaltingException { get; private set; }
            
        public Status CurrentStatus { get; private set; }

        public void MoveToPreProcessing()
        {
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
            CurrentStatus = Status.DispatchModelUpdates;
        }

        public void MoveToHalted(Exception exception)
        {
            HaltingException = exception;
            CurrentStatus = Status.Halted;
        }

        public void MoveToIdle()
        {
            CurrentStatus = Status.Idle;
        }
    }
}