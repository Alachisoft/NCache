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
    class TestIpAddressType
    {
        [Test]
        public void Test()
        {
            const string test = "SomeIp ::= IpAddress";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.IpAddress);

            IpAddressType i = new IpAddressType("module", "name", lexer);
        }
    }
}
