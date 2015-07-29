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
using Esp.Net.Reactive;
using NUnit.Framework;
using Shouldly;

namespace Esp.Net
{
    [TestFixture]
    public class RouterTests
    {
        public class TestModel
        {
            public TestModel()
            {
                Id = Guid.NewGuid();
            }
            public Guid Id { get; private set; }
            public int AnInt { get; set; }
            public string AString { get; set; }
            public decimal ADecimal { get; set; }
        }

        public class BaseEvent { }
        public class Event1 : BaseEvent { }
        public class Event2 : BaseEvent { }
        public class Event3 : BaseEvent { }
        public class EventWithoutBaseType { }

        private TestModel _model;

        private Router _router;

        [SetUp]
        public void SetUp()
        {
            _model = new TestModel();
            _router = new Router(ThreadGuard.Default);
            _router.RegisterModel(_model.Id, _model);
        }

        [Test]
        public void CanObserveEventsByBaseType()
        {
            // this is effictively what we're doing here with covariance  
            // IEventContext<TestModel, BaseEvent> ec1 = new EventContext<TestModel, Event1>(null, null);
            // For example check out the .Observe() method, it receives an event context 
            // typed for BaseEvent not Event1.
            var receivedEvents1 = new List<BaseEvent>();
            var receivedEvents2 = new List<BaseEvent>();
            _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event1))
                .Observe((model, baseEvent, context) =>
                {
                    receivedEvents1.Add(baseEvent);
                });
            _router.GetEventObservable<TestModel, Event1, BaseEvent>(_model.Id)
                .Observe((model, baseEvent, context) =>
                {
                    receivedEvents2.Add(baseEvent);
                });
            _router.PublishEvent(_model.Id, new Event1());
            receivedEvents1.Count.ShouldBe(1);
            receivedEvents2.Count.ShouldBe(1);
        }

        [Test]
        public void WhenObservingByBaseTypeItThrowsIfSubTypeDoesntDeriveFromBase()
        {
            Assert.Throws<InvalidOperationException>(() => {
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(EventWithoutBaseType)).Observe((m, e, c) => { });
            });
        }

        [Test]
        public void CanConcatEventStreams()
        {
            var receivedEvents = new List<BaseEvent>();
            var stream = EventObservable.Concat(
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event1)),
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event2)),
                _router.GetEventObservable<TestModel, BaseEvent>(_model.Id, typeof(Event3))
            );
            stream.Observe((model, baseEvent, context) =>
            {
                receivedEvents.Add(baseEvent);
            });
            _router.PublishEvent(_model.Id, new Event1());
            _router.PublishEvent(_model.Id, new Event2());
            _router.PublishEvent(_model.Id, new Event3());
            receivedEvents.Count.ShouldBe(3);
        }

        public class DelegeatePreEventProcessor : IPreEventProcessor<TestModel>
        {
            private readonly Action<TestModel> _process;

            public DelegeatePreEventProcessor(Action<TestModel> process)
            {
                _process = process;
            }

            public void Process(TestModel model)
            {
                _process(model);
            }
        }

        public class DelegeatePostEventProcessor : IPostEventProcessor<TestModel>
        {
            private readonly Action<TestModel> _process;

            public DelegeatePostEventProcessor(Action<TestModel> process)
            {
                _process = process;
            }

            public void Process(TestModel model)
            {
                _process(model);
            }
        }
    }
}