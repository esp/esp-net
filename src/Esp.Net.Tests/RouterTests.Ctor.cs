using System;
using NUnit.Framework;

namespace Esp.Net
{
    public partial class RouterTests
    {
        public class Ctor
        {
            [Test]
            public void ThrowsIfThreadGuardNull()
            {
                Assert.Throws<ArgumentNullException>(() => new Router(null));
            }
        }
    }
}