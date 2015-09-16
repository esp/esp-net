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

#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Diagnostics;

// TODO threading!! it's completely unsafe atm given the introduction of IObservable

namespace Esp.Net.Workflow
{
    public interface IWorkflowInstanceContext
    {
        bool IsCanceled { get; }
        void Cancel();
    }

    public static class WorkflowRouterExt
    {
        public static WorkflowBuilder<TModel, DefatultWorkflowInstanceContext, TInitialEvent> ConfigureWorkflow<TModel, TInitialEvent>(
            this IRouter router
        )
        {
            return new WorkflowBuilder<TModel, DefatultWorkflowInstanceContext, TInitialEvent>(
                router, 
                (m, e, c) => new DefatultWorkflowInstanceContext()
            );
        }

        public static WorkflowBuilder<TModel, TWorkflowContext, TInitialEvent> ConfigureWorkflow<TModel, TWorkflowContext, TInitialEvent>(
            this IRouter router,
            Func<TModel, TInitialEvent, IEventContext, TWorkflowContext> contextFactory
        ) 
            where TWorkflowContext : IWorkflowInstanceContext
        {
            return new WorkflowBuilder<TModel, TWorkflowContext, TInitialEvent>(router, contextFactory);
        }
    }

    public interface IWorkflow<TModel, TWorkflowContext>
        where TWorkflowContext : IWorkflowInstanceContext
    {
        IWorkflowInstance<TModel, TWorkflowContext> CreateInstance();
    }

    public interface IWorkflowInstance<TModel, TWorkflowContext> : IDisposable
        where TWorkflowContext : IWorkflowInstanceContext
    {
        void Run(
            Guid modelId,
            TModel currentModel, 
            TWorkflowContext context,
            Action<TModel, TWorkflowContext, Exception> onError,
            Action<TModel, TWorkflowContext> onCompleted
        );
    }

    public class WorkflowBuilder<TModel, TWorkflowContext, TInitialEvent> 
        where TWorkflowContext : IWorkflowInstanceContext
    {
        private readonly IRouter _router;
        private readonly Func<TModel, TInitialEvent, IEventContext, TWorkflowContext> _contextFactory;
        private readonly List<Step<TModel, TWorkflowContext>> _steps = new List<Step<TModel, TWorkflowContext>>();

        public WorkflowBuilder(
            IRouter router,
            Func<TModel, TInitialEvent, IEventContext, TWorkflowContext> contextFactory)
        {
            _router = router;
            _contextFactory = contextFactory;
        }

        public WorkflowBuilder<TModel, TWorkflowContext, TInitialEvent> SelectMany<TResult>(
            Func<TModel, TWorkflowContext, IObservable<TResult>> observableFactory,
            Action<TModel, TWorkflowContext, TResult> onResultsReceived
        )
        {
            var step = new ObservableStep<TModel, TWorkflowContext, TResult>(_router, observableFactory, onResultsReceived);
            _steps.Add(step);
            return this;
        }

        public WorkflowBuilder<TModel, TWorkflowContext, TInitialEvent> Do(
            Action<TModel, TWorkflowContext> action
        )
        {
            var step = new SyncStep<TModel, TWorkflowContext>(action);
            _steps.Add(step);
            return this;
        }
        
        public IWorkflow<TModel, TWorkflowContext> Create()
        {
            return new Workflow<TModel, TWorkflowContext>(_steps);
        }

        public IDisposable Run(Guid modelId, Action<TModel, TWorkflowContext, Exception> onError, Action<TModel, TWorkflowContext> onCompleted)
        {
            var disposables = new DictionaryDisposable<Guid>();
            IWorkflow<TModel, TWorkflowContext> workflow = Create();
            var eventSubscription = _router.GetEventObservable<TModel, TInitialEvent>(modelId).Observe((model, e, eventContext) =>
            {
                var instanceId = Guid.NewGuid();
                IWorkflowInstance<TModel, TWorkflowContext> workflowInstance = workflow.CreateInstance();
                TWorkflowContext workflowInstanceContext = _contextFactory(model, e, eventContext);
                disposables.Add(instanceId, workflowInstance);
                workflowInstance.Run(
                    modelId,
                    model, 
                    workflowInstanceContext, 
                    (model1, context, ex) =>
                    {
                        disposables.Remove(instanceId);
                        onError(model1, workflowInstanceContext, ex);
                    },
                    (model1, context) =>
                    {
                        disposables.Remove(instanceId);
                        onCompleted(model1, workflowInstanceContext);
                    }
                );
            });
            disposables.Add(Guid.NewGuid(), eventSubscription);
            return disposables;
        }
    }

    internal class Workflow<TModel, TWorkflowContext> : DisposableBase, IWorkflow<TModel, TWorkflowContext>
        where TWorkflowContext : IWorkflowInstanceContext
    {
        private readonly List<Step<TModel, TWorkflowContext>> _steps;

        public Workflow(List<Step<TModel, TWorkflowContext>> steps)
        {
            _steps = steps;
        }

        public IWorkflowInstance<TModel, TWorkflowContext> CreateInstance()
        {
            var firstStep = _steps[0];
            for (int i = 1; i < _steps.Count; i++)
            {
                firstStep.Next = _steps[i];
            }
            return new WorkflowInstance(firstStep);
        }

        // TODO it's entirely possible that a workflow instance is never disposed, it may just run it's course. 
        // however it if it's disposed before this point further should't run.
        private class WorkflowInstance : DisposableBase, IWorkflowInstance<TModel, TWorkflowContext>
        {
            private readonly Step<TModel, TWorkflowContext> _firstStep;
            private Action<TModel, TWorkflowContext, Exception> _onError;
            private readonly Queue<Action<TModel, TWorkflowContext>> _queue = new Queue<Action<TModel, TWorkflowContext>>();
            private bool _purging;
            private Action<TModel, TWorkflowContext> _onCompleted;

            public WorkflowInstance(Step<TModel, TWorkflowContext> firstStep)
            {
                _firstStep = firstStep;
            }

            public void Run(
                Guid modelId,
                TModel currentModel, 
                TWorkflowContext context,
                Action<TModel, TWorkflowContext, Exception> onError,
                Action<TModel, TWorkflowContext> onCompleted
            )
            {
                _onError = onError;
                _onCompleted = onCompleted;
                _queue.Enqueue(CreateStep(modelId, _firstStep));
                PurgeQueue(currentModel, context);
            }

            private Action<TModel, TWorkflowContext> CreateStep(Guid modelId, Step<TModel, TWorkflowContext> step)
            {
                return (currentModel, context) =>
                {
                    if (step.Type == StepType.Async)
                    {
                        IDisposable stepDisposable = EspDisposable.Empty;
                        // note that the step may yield multiple times and we just stay subscribed until it 
                        // errors or completes. This means we may run a step once, then run subsequent steps 
                        // multiple times. 
                        stepDisposable = step.GetExecuteStream(modelId, currentModel, context).Subscribe(latestModel =>
                        {
                            if (step.Next != null)
                            {
                                _queue.Enqueue(CreateStep(modelId, step.Next));
                                PurgeQueue(latestModel, context);
                            }
                        },
                        ex =>
                        {
                            if (_onError == null)
                            {
                                throw ex;
                            }
                           // _onError(model, context, ex);
                        },
                        () =>
                        {
                            // TODO need to dispose of child steps
                            // need to finish instance workflow, i.e. hook back into PurgeQueue or lift that 
                            // workflow so we can poke the instance and get it to wind up in this case.
                        });
                        AddDisposable(stepDisposable);
                    }
                    else
                    {
                        step.Execute(currentModel, context);
                        if (step.Next != null)
                        {
                            _queue.Enqueue(CreateStep(modelId, step.Next));
                            PurgeQueue(currentModel, context);
                        }
                    }
                };
            }

            private void PurgeQueue(TModel currentModel, TWorkflowContext context)
            {
                Debug.Assert(!_purging);
                _purging = true;
                try
                {
                    var hasItems = _queue.Count > 0;
                    while (hasItems)
                    {
                        var action = _queue.Dequeue();
                        action(currentModel, context);
                        hasItems = _queue.Count > 0;
                    }
                    _onCompleted(currentModel, context);
                }
                finally
                {
                    _purging = false;
                }
            }
        }
    }

    public class DefatultWorkflowInstanceContext : IWorkflowInstanceContext
    {
        private bool _isCanceled;

        public bool IsCanceled
        {
            get { return _isCanceled; }
        }

        public void Cancel()
        {
            if (_isCanceled) throw new InvalidOperationException("Already canceled");
            _isCanceled = true;
        }
    }
}
#endif