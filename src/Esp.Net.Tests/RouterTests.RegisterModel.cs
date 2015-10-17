using System;
using NUnit.Framework;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class RegisterModel : RouterTests
        {
            [Test]
            public void ThrowsIfModelIdNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.AddModel(null, new object()));
            }

            [Test]
            public void ThrowsIfPreEventProcessorNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.AddModel(Guid.NewGuid(), new object(), (IPreEventProcessor<object>)null));
            }

            [Test]
            public void ThrowsIfPostEventProcessorNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.AddModel(Guid.NewGuid(), new object(), (IPostEventProcessor<object>)null));
            }

            [Test]
            public void ThrowsIfModelNull()
            {
                Assert.Throws<ArgumentNullException>(() => _router.AddModel(Guid.NewGuid(), (object)null));
            }

            [Test]
            public void ThrowsIfModelAlreadyRegistered()
            {
                Assert.Throws<ArgumentException>(() => _router.AddModel(_model1.Id, new TestModel()));
            }
        }
    }
}