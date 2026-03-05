using System.Threading;
using Lextm.Common;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class WatchdogTestFixture
    {
        [Test]
        public void Test()
        {
            int count = 0;
            Watchdog dog = new Watchdog(100d);
            dog.Bark += delegate
                            {
                                count++;
                            };
            dog.Enabled = true;
            dog.KeepBarking = false;
            dog.Feed();
            dog.Feed();
            dog.Feed();
            dog.Feed();
            Thread.Sleep(200);
            Assert.AreEqual(1, count);
            Thread.Sleep(120);
            dog.Enabled = false;
            Assert.IsFalse(dog.Enabled);
            Assert.IsFalse(dog.KeepBarking);
            // TODO: 100% coverage in this way?
            dog.Feed();
            Assert.AreEqual(1, count);
        }
    }
}
