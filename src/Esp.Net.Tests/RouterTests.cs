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
using System.Collections.ObjectModel;
using NUnit.Framework;

namespace Esp.Net
{
    [TestFixture]
    public partial class RouterTests
    {
        private Router _router;
        private StubRouterDispatcher _routerDispatcher;

        private TestModel _model1;
        private StubModelProcessor _model1PreEventProcessor;
        private StubModelProcessor _model1PostEventProcessor;
        private GenericModelEventProcessor<TestModel> _model1EventProcessor;
        private GenericModelEventProcessor<TestModel> _model1EventProcessor2;
        private TestModelController _model1Controller;

        private TestModel _model2;
        private StubModelProcessor _model2PreEventProcessor;
        private StubModelProcessor _model2PostEventProcessor;
        private GenericModelEventProcessor<TestModel> _model2EventProcessor;
        private TestModelController _model2Controller;
        private TerminalErrorHandler _terminalErrorHandler;

        private TestModel3 _model3;
        private TestModel4 _model4;
        private TestModel5 _model5;

        private const int EventProcessor1Id = 1;
        private const int EventProcessor2Id = 2;
        private const int EventProcessor3Id = 3;

        [SetUp]
        public virtual void SetUp()
        {
            _routerDispatcher = new StubRouterDispatcher();
            _terminalErrorHandler = new TerminalErrorHandler();
            _router = new Router(_routerDispatcher, _terminalErrorHandler);

            _model1 = new TestModel();
            _model1PreEventProcessor = new StubModelProcessor();
            _model1PostEventProcessor = new StubModelProcessor();
            _router.AddModel(_model1.Id, _model1, _model1PreEventProcessor, _model1PostEventProcessor);
            _model1EventProcessor = new GenericModelEventProcessor<TestModel>(_router, _model1.Id, EventProcessor1Id);
            _model1EventProcessor2 = new GenericModelEventProcessor<TestModel>(_router, _model1.Id, EventProcessor2Id);
            _model1Controller = new TestModelController(_router, _model1.Id);

            _model2 = new TestModel();
            _model2PreEventProcessor = new StubModelProcessor();
            _model2PostEventProcessor = new StubModelProcessor();
            _router.AddModel(_model2.Id, _model2, _model2PreEventProcessor, _model2PostEventProcessor);
            _model2EventProcessor = new GenericModelEventProcessor<TestModel>(_router, _model2.Id, EventProcessor3Id);
            _model2Controller = new TestModelController(_router, _model2.Id);

            _model3 = new TestModel3();
            _router.AddModel(_model3.Id, _model3);

            _model4 = new TestModel4();
            _router.AddModel(_model4.Id, _model4);

            _model5 = new TestModel5();
            _router.AddModel(_model5.Id, _model5);
        }

        protected void PublishEventWithMultipeSubsequentEvents(int numberOfSubsequentEvents)
        {
            _router
                .GetEventObservable<TestModel, Event1>(_model1.Id)
                .Where((m, e) => e.Payload != "subsequent")
                .Observe(
                    (model, @event) =>
                    {
                        for (int i = 0; i < numberOfSubsequentEvents; i++)
                        {
                            _router.PublishEvent(_model1.Id, new Event1("subsequent"));
                        }
                    }
                );
            _router.PublishEvent(_model1.Id, new Event1());
        }

        public class TestModel
        {
            public TestModel()
            {
                Id = Guid.NewGuid();
                SubTestModel = new SubTestModel();
            }
            public Guid Id { get; private set; }
            public bool ControllerShouldRemove { get; set; }
            public SubTestModel SubTestModel { get; private set; }
        }

        public class SubTestModel
        {

        }

        public class TestModel3
        {
            public TestModel3()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
        }

        public class TestModel4 : ICloneable<TestModel4>
        {
            public TestModel4()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; private set; }

            public bool IsClone { get; private set; }

            public TestModel4 Clone()
            {
                return new TestModel4() { Id = Id, IsClone = true };
            }
        }

        public class TestModel5 : IPreEventProcessor, IPostEventProcessor
        {
            public TestModel5()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; private set; }

            public int PreProcessorInvocationCount { get; private set; }

            public int PostProcessorInvocationCount { get; private set; }

            void IPreEventProcessor.Process()
            {
                PreProcessorInvocationCount++;
            }

            void IPostEventProcessor.Process()
            {
                PostProcessorInvocationCount++;
            }
        }

        public class BaseEvent
        {
            public bool ShouldCancel { get; set; }
            public ObservationStage CancelAtStage { get; set; }
            public int CancelAtEventProcesserId { get; set; }

            public bool ShouldCommit { get; set; }
            public ObservationStage CommitAtStage { get; set; }
            public int CommitAtEventProcesserId { get; set; }

            public bool ShouldRemove { get; set; }
            public ObservationStage RemoveAtStage { get; set; }
            public int RemoveAtEventProcesserId { get; set; }
        }

        public class Event1 : BaseEvent
        {
            public Event1()
            {
            }

            public Event1(string payload)
            {
                Payload = payload;
            }

            public string Payload { get; private set; }
        }

        public class Event2 : BaseEvent { }

        public class Event3 : BaseEvent { }

        public class AnExecutedEvent
        {
            int Payload { get; set; }
        }

        public class StubModelProcessor : IPreEventProcessor<TestModel>, IPostEventProcessor<TestModel>
        {
            private readonly List<Action<TestModel>> _actions = new List<Action<TestModel>>();

            public int InvocationCount { get; private set; }

            public void Process(TestModel model)
            {
                InvocationCount++;

                foreach (Action<TestModel> action in _actions)
                {
                    action(model);
                }
            }

            public void RegisterAction(Action<TestModel> action)
            {
                _actions.Add(action);
            }
        }

        // this generic event processor exists to:  
        // * record what events it receives during execution
        // * push events through the flow as requested by tests
        // * run actions during the workflow as requested by the tests
        public class GenericModelEventProcessor<TModel>
        {
            private readonly Guid _modelId;
            private readonly int _id;
            private readonly IRouter _router;

            public GenericModelEventProcessor(IRouter router, Guid modelId, int id)
            {
                _modelId = modelId;
                _id = id;
                _router = router;

                Event1Details = ObserveEvent<Event1>();
                Event2Details = ObserveEvent<Event2>();
                Event3Details = ObserveEvent<Event3>();
            }

            public EventObservationDetails<Event1> Event1Details { get; private set; }
            public EventObservationDetails<Event2> Event2Details { get; private set; }
            public EventObservationDetails<Event3> Event3Details { get; private set; }

            private EventObservationDetails<TEvent> ObserveEvent<TEvent>() where TEvent : BaseEvent
            {
                var observationDetails = new EventObservationDetails<TEvent>
                {
                    PreviewStage = WireUpObservationStage<TEvent>(ObservationStage.Preview),
                    NormalStage = WireUpObservationStage<TEvent>(ObservationStage.Normal),
                    CommittedStage = WireUpObservationStage<TEvent>(ObservationStage.Committed)
                };
                return observationDetails;
            }

            private EventObservationStageDetails<TEvent> WireUpObservationStage<TEvent>(ObservationStage stage) where TEvent : BaseEvent
            {
                var details = new EventObservationStageDetails<TEvent>(stage);
                details.ObservationDisposable = _router.GetEventObservable<TModel, TEvent>(_modelId, details.Stage)
                    .Observe(
                        (model, @event, context) =>
                        {
                            details.ReceivedEvents.Add(@event);
                            var shouldCancel = @event.ShouldCancel && stage == @event.CancelAtStage && @event.CancelAtEventProcesserId == _id;
                            if (shouldCancel)
                            {
                                context.Cancel();
                            }
                            var shouldCommit = @event.ShouldCommit && stage == @event.CommitAtStage && @event.CommitAtEventProcesserId == _id;
                            if (shouldCommit)
                            {
                                context.Commit();
                            }
                            var shouldRemove = @event.ShouldRemove && stage == @event.RemoveAtStage && @event.RemoveAtEventProcesserId == _id;
                            if (shouldRemove)
                            {
                                _router.RemoveModel(_modelId);
                            }
                            foreach (Action<TModel, TEvent, IEventContext> action in details.Actions)
                            {
                                action(model, @event, context);
                            }
                        },
                        () => details.StreamCompletedCount++);
                return details;
            }

            public class EventObservationDetails<TEvent>
            {
                public EventObservationStageDetails<TEvent> PreviewStage { get; set; }
                public EventObservationStageDetails<TEvent> NormalStage { get; set; }
                public EventObservationStageDetails<TEvent> CommittedStage { get; set; }
            }

            public class EventObservationStageDetails<TEvent>
            {
                private readonly List<Action<TModel, TEvent, IEventContext>> _actions;
                public EventObservationStageDetails(ObservationStage stage)
                {
                    Stage = stage;
                    ReceivedEvents = new List<TEvent>();
                    _actions = new List<Action<TModel, TEvent, IEventContext>>();
                    Actions = new ReadOnlyCollection<Action<TModel, TEvent, IEventContext>>(_actions);
                }
                public ObservationStage Stage { get; private set; }
                public List<TEvent> ReceivedEvents { get; private set; }
                public IDisposable ObservationDisposable { get; set; }
                public int StreamCompletedCount { get; set; }
                public IList<Action<TModel, TEvent, IEventContext>> Actions { get; private set; }
                public void RegisterAction(Action<TModel, TEvent> action)
                {
                    _actions.Add((m, e, c) => action(m, e));
                }
                public void RegisterAction(Action<TModel, TEvent, IEventContext> action)
                {
                    _actions.Add(action);
                }
            }
        }

        public class TestModelController
        {
            private readonly List<Action<TestModel>> _actions = new List<Action<TestModel>>();

            public TestModelController(IRouter router, Guid modelId)
            {
                ReceivedModels = new List<TestModel>();
                ModelObservationDisposable = router
                    .GetModelObservable<TestModel>(modelId)
                    .Observe(
                        model =>
                        {
                            ReceivedModels.Add(model);
                            if (model.ControllerShouldRemove)
                                router.RemoveModel(modelId);
                            foreach (Action<TestModel> action in _actions)
                            {
                                action(model);
                            }
                        },
                        () =>
                        {
                            StreamCompletedCount++;
                        }
                    );
            }

            public IDisposable ModelObservationDisposable { get; private set; }

            public List<TestModel> ReceivedModels { get; private set; }

            public int StreamCompletedCount { get; private set; }

            public void RegisterAction(Action<TestModel> action)
            {
                _actions.Add(action);
            }
        }

        public class TerminalErrorHandler : ITerminalErrorHandler
        {
            public TerminalErrorHandler()
            {
                Errors = new List<Exception>();
            }

            public List<Exception> Errors { get; private set; }

            public void OnError(Exception exception)
            {
                Errors.Add(exception);
            }
        }
    }
}