using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Lextm.SharpSnmpLib.Mib;

namespace Lextm.SharpSnmpLib.Tests
{
    class TestBitsType
    {
        [Test]
        public void Test()
        {
            const string test = "SomeEnum ::= BITS {first(1), second(2)}";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Bits);

            BitsType i = new BitsType("module", "name", lexer);
            Assert.AreEqual("first(1)", i[1]);
        }
    }
}
