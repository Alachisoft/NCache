using System.IO;
using Lextm.SharpSnmpLib.Mib;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    class TestIntegerType
    {
        [Test]
        public void TestEnumerable()
        {
            const string test = "SomeEnum ::= INTEGER {first(1), second(2)}";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            IntegerType i = new IntegerType("module", "name", lexer);
            Assert.IsTrue(i.IsEnumeration);
            Assert.AreEqual("first(1)", i[1]);
        }

        [Test]
        public void TestRanges()
        {
            const string test = "SomeEnum ::= INTEGER (8 |10 ..20| 31..60 )";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            IntegerType i = new IntegerType("module", "name", lexer);
            Assert.IsFalse(i.IsEnumeration);
            Assert.IsTrue(i.Contains(8));
            Assert.IsTrue(i.Contains(10));
            Assert.IsTrue(i.Contains(15));
            Assert.IsTrue(i.Contains(20));
            Assert.IsTrue(i.Contains(35));
            Assert.IsFalse(i.Contains(4));
            Assert.IsFalse(i.Contains(-9));
            Assert.IsFalse(i.Contains(25));
            Assert.IsFalse(i.Contains(61));
        }

        [Test]
        public void TestOverlappingRanges1()
        {
            const string test = "SomeEnum ::= INTEGER (8 | 5 .. 20 |31 .. 60 )";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            Assert.Throws<MibException>(() => new IntegerType("module", "name", lexer));
        }

        [Test]
        public void TestOverlappingRanges2()
        {
            const string test = "SomeEnum ::= INTEGER (8 | 8 .. 20 | 31 .. 60 )";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            Assert.Throws<MibException>(() => new IntegerType("module", "name", lexer));
        }

        [Test]
        public void TestOverlappingRanges3()
        {
            const string test = "SomeEnum ::= INTEGER (8 | 8 | 31 .. 60 )";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            Assert.Throws<MibException>(() => new IntegerType("module", "name", lexer));
        }

        [Test]
        public void TestOverlappingRanges4()
        {
            const string test = "SomeEnum ::= INTEGER (8 | 5..20 | 31 .. 60 )";
            Lexer lexer = new Lexer();
            StringReader reader = new StringReader(test);
            lexer.Parse(reader);
            string name = lexer.GetNextSymbol().ToString();
            lexer.GetNextSymbol().Expect(Symbol.Assign);
            lexer.GetNextSymbol().Expect(Symbol.Integer);

            Assert.Throws<MibException>(() => new IntegerType("module", "name", lexer));
        }
    }
}
