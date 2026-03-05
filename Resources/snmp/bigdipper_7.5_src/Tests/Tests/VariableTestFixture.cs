/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 12:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;

#pragma warning disable 1591
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class VariableTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new Variable((ObjectIdentifier)null, null));
            
            Assert.Throws<ArgumentNullException>(() => Variable.Transform((IList<Variable>)null));
            Assert.Throws<ArgumentNullException>(() => Variable.Transform((Sequence)null));
            
            var seq = new Sequence(null, new OctetString("test"));
            Assert.Throws<ArgumentException>(() => Variable.Transform(seq));
            
            var seq2 = new Sequence(null, new Sequence(null, new OctetString("test")));
            Assert.Throws<ArgumentException>(() => Variable.Transform(seq2));
            
            var seq3 = new Sequence(null, new Sequence(null, new OctetString("test"), new Sequence((byte[])null)));
            Assert.Throws<ArgumentException>(() => Variable.Transform(seq3));
        }
        
        [Test]
        public void TestToString()
        {
            var v = new Variable(new uint[] {1, 3, 6});
            Assert.AreEqual("Variable: Id: .1.3.6; Data: Null", v.ToString());
        }
        
        [Test]
        public void TestToBytes()
        {
            Variable v = new Variable(
                    new ObjectIdentifier(new uint[] {1,3,6,1,4,1,2162,1001,21,0}),
                    new OctetString("TrapTest"));
            List<Variable> vList = new List<Variable> {v};

            Sequence varbindSection = Variable.Transform(vList);
            Assert.AreEqual(1, varbindSection.Length);
            Sequence varbind = (Sequence)varbindSection[0];
            Assert.AreEqual(2, varbind.Length);
        }

        [Test]
        // [ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestConstructor()
        {
            List<Variable> vList = new List<Variable>
                                       {
                                           new Variable(
                                               new ObjectIdentifier(new uint[] {1, 3, 6, 1, 2, 1, 2, 2, 1, 22, 1}),
                                               new ObjectIdentifier(new uint[] {0, 0}))
                                       };

            Variable.Transform(vList);
        }
    }
}
#pragma warning restore 1591