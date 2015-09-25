using System;
using NUnit.Framework;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class Ctor
        {
            [Test]
            public void ThrowsIfIRouterDispatcherNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Router((IRouterDispatcher)null));
            }
        }
    }
}