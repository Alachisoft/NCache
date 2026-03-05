using System;
using Lextm.SharpSnmpLib.Mib;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class SearchResultTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new SearchResult(null, new uint[0]));
            Assert.Throws<ArgumentNullException>(() => new SearchResult(new Definition(new uint[] {1,3,6}, "test", null, "test", "test"), null));
            var result = new SearchResult(new Definition(new uint[] {1, 3, 6}, "test", null, "test", "test"));
            Assert.AreEqual(string.Empty, result.AlternativeText);
        }
    }
}
