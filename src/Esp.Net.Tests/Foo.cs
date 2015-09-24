using System;
using System.Reactive.Linq;
using NUnit.Framework;

namespace Esp.Net
{
    [TestFixture]
    public class Foo
    {
        [Test]
        public void Foo1()
        {
            var router = new Router<TestModel>(new TestModel());
            router.GetEventObservable<int>().Observe((m, e) =>
            {
                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(i =>
                {
                    router.RunAction(() =>
                    {
                        
                    });
                    router.RunAction((model) =>
                    {
                        model.Count++;
                    });
                });
            });
        }  

        public class TestModel
        {
            public int Count { get; set; }
        }
    }


}