#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Esp.Net.Model;
using System.Reactive.Linq;

namespace Esp.Net.Concurrency
{
    public static class WorkItemRouterExt
    {
        public static WorkItemBuilder<TModel> CreateWorkItemBuilder<TModel>(this IRouter<TModel> router)
        {
            return new WorkItemBuilder<TModel>(router);
        }
    }

    public interface IWorkItem<in TModel>
    {
        IWorkItemInstance<TModel> CreateInstance();
    }

    public interface IWorkItemInstance<in TModel> : IDisposable
    {
        void Run(TModel currentModel, Action<Exception> onError = null);
    }

    public class WorkItemBuilder<TModel>
    {
        private readonly IRouter<TModel> _router;
        private readonly List<Step<TModel>> _steps = new List<Step<TModel>>(); 

        public WorkItemBuilder(IRouter<TModel> router)
        {
            _router = router;
        }

        public WorkItemBuilder<TModel> AddStep<TResult>(
            Func<TModel, IObservable<TResult>> observableFactory,
            Action<TModel, TResult> onResultsReceived
        )
        {
            var step = new ObservableStep<TModel, TResult>(_router, observableFactory, onResultsReceived);
            _steps.Add(step);
            return this;
        }

        public IWorkItem<TModel> CreateWorkItem()
        {
            return new WorkItem<TModel>(_steps);
        }
    }

    public class WorkItem<TModel> : DisposableBase, IWorkItem<TModel>
    {
        private readonly List<Step<TModel>> _steps;

        public WorkItem(List<Step<TModel>> steps)
        {
            _steps = steps;
        }

        public IWorkItemInstance<TModel> CreateInstance()
        {
            var firstStep = _steps[0];
            for (int i = 1; i < _steps.Count; i++)
            {
                firstStep.Next = _steps[i];
            }
            return new WorkItemInstance(firstStep);
        }

        // it's entirely possible that a WorkItem instance is never disposed, it may just run it's course. 
        // however it if it's disposed before this point father step won't be run.
        private class WorkItemInstance : DisposableBase, IWorkItemInstance<TModel>
        {
            private readonly Step<TModel> _firstStep;
            private Action< Exception> _onError;
            private readonly Queue<Action<TModel>> _queue = new Queue<Action<TModel>>();
            private bool _purging;

            public WorkItemInstance(Step<TModel> firstStep)
            {
                _firstStep = firstStep;
            }

            public void Run(TModel currentModel, Action<Exception> onError = null)
            {
                _onError = onError;
                _queue.Enqueue(CreateStep(_firstStep));
                PurgeQueue(currentModel);
            }

            private Action<TModel> CreateStep(Step<TModel> step)
            {
                return (currentModel) =>
                {
                    if (step.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        // note that the step may yield multiple times and we just stay subscribed until it 
                        // errors or completes. This means we may run a step once, then run subsequent steps 
                        // multiple times. 
                        stepDisposable = step.GetExecuteStream(currentModel).Subscribe(latestModel =>
                        {
                            if (step.Next != null)
                            {
                                _queue.Enqueue(CreateStep(step.Next));
                                PurgeQueue(latestModel);
                            }
                        },
                        ex =>
                        {
                            if (_onError == null)
                            {
                                throw ex;
                            }
                            _onError(ex);
                        },
                        () =>
                        {
                            // need to dispose of child steps        
                        });
                        AddDisposable(stepDisposable);
                    }
                    else
                    {
                        step.Execute(currentModel);
                        if (step.Next != null)
                        {
                            _queue.Enqueue(CreateStep(step.Next));
                            PurgeQueue(currentModel);
                        }
                    }
                };
            }

            private void PurgeQueue(TModel currentModel)
            {
                Debug.Assert(!_purging);
                _purging = true;
                try
                {
                    var hasItems = _queue.Count > 0;
                    while (hasItems)
                    {
                        var action = _queue.Dequeue();
                        action(currentModel);
                        hasItems = _queue.Count > 0;
                    }
                }
                finally
                {
                    _purging = false;
                }
            }
        }
    }
}
#endif