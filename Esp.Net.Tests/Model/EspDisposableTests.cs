using System;
using NUnit.Framework;

namespace Esp.Net.Model
{
    [TestFixture]
    public sealed class EspDisposableTests
    {
        [Test]
        public void ShouldThrowWithNullAction()
        {
            Assert.Throws<ArgumentNullException>(() => EspDisposable.Create(null));
        }
    }
}
