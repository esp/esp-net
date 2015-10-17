using System;
using NUnit.Framework;

namespace Esp.Net.Utils
{
    [TestFixture]
    public class ReflectionHelperTests
    {
        public class Event { }
        public class Event_ { }

        public class Event1 : Event { }
        public class Event2 : Event1 { }
        public class Event3 : Event2 { }
        public class EventA : Event { }
        public class Event__ : Event_ { }
        public class Event___ : Event__ { }

        [Test]
        public void TryGetCommonBaseType_WhenTypesShareCommonTypeItIsReturned()
        {
            Type commonType; 
            bool hasCommonType = ReflectionHelper.TryGetCommonBaseType(out commonType,  typeof (Event1), typeof (EventA));
            Assert.AreEqual(typeof(Event), commonType);
            Assert.IsTrue(hasCommonType);

            hasCommonType = ReflectionHelper.TryGetCommonBaseType(out commonType, typeof(Event1), typeof(EventA), typeof(Event3));
            Assert.AreEqual(typeof(Event), commonType);
            Assert.IsTrue(hasCommonType);
        }

        [Test]
        public void TryGetCommonBaseType_WhenTypesDoNotShareCommonTypeNullIsReturned()
        {
            Type commonType;
            bool hasCommonType = ReflectionHelper.TryGetCommonBaseType(out commonType, typeof(Event__), typeof(Event3));
            Assert.IsNull(commonType);
            Assert.IsFalse(hasCommonType);
        }

        [Test]
        public void SharesBaseType_WhenTypesShareCommonTypeReturnsTrue()
        {
            bool sharesCommonType = ReflectionHelper.SharesBaseType(typeof(Event), typeof (Event1), typeof (EventA));
            Assert.IsTrue(sharesCommonType);
        }

        [Test]
        public void SharesBaseType_WhenTypesDoNotShareCommonTypeReturnsFalse()
        {
            bool sharesCommonType = ReflectionHelper.SharesBaseType(typeof(Event), typeof(Event__), typeof(Event3));
            Assert.IsFalse(sharesCommonType);
        }
    }
}