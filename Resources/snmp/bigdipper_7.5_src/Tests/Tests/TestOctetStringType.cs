using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Lextm.SharpSnmpLib.Mib;
using System.IO;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    class TestOctetStringType
    {
        [Test]
        public void TestRanges()
        {
            const string test = "SomeEnum ::= OCTET STRING ( SIZE (5 | 8 .. 20 | 31 .. 60 ))";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Octet);
            lexer.GetNextSymbol().Expect(Symbol.String);

            OctetStringType i = new OctetStringType("module", "name", lexer);
            Assert.IsTrue(i.Contains(8));
            Assert.IsTrue(i.Contains(5));
            Assert.IsTrue(i.Contains(15));
            Assert.IsTrue(i.Contains(20));
            Assert.IsTrue(i.Contains(35));
            Assert.IsFalse(i.Contains(4));
            Assert.IsFalse(i.Contains(-9));
            Assert.IsFalse(i.Contains(25));
            Assert.IsFalse(i.Contains(61));
        }

        [Test]
        public void TestNegative()
        {
            const string test = "SomeEnum ::= OCTET STRING ( SIZE (8 | -5 .. 20 | 31 .. 60 ))";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Octet);
            lexer.GetNextSymbol().Expect(Symbol.String);

            Assert.Throws<MibException>(delegate { new OctetStringType("module", "name", lexer); });
        }
    }
}
