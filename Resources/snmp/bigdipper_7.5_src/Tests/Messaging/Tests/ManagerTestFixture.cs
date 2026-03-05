/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 11:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using NUnit.Framework;

#pragma warning disable 1591
namespace Lextm.SharpSnmpLib.Messaging.Tests
{
    [TestFixture]
    public class ManagerTestFixture
    {
        [Test]
        public void TestGetTable()
        {
            //IList<Variable> list = new List<Variable>();
            //TableCollection result = new TableCollection(new ObjectIdentifier("1.2"), list);
            //Assert.AreEqual(0, result.IndexCount);
        }

        [Test]
        public void TestGetTable1()
        {
            //IList<Variable> list = new List<Variable>();
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.1.1"), new Integer32(12)));
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.1.2"), new Integer32(24)));
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.1.3"), new Integer32(36)));
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.2.1"), new Integer32(1)));
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.2.2"), new Integer32(2)));
            //list.Add(new Variable(new ObjectIdentifier("1.2.1.2.3"), new Integer32(3)));
            //TableCollection result = new TableCollection(new ObjectIdentifier("1.2"), list);
            //Assert.AreEqual(1, result.IndexCount);
            //Assert.AreEqual(new Integer32(12), result.GetVariableAt(0, 0).Data);
            //Assert.AreEqual(new Integer32(36), result.GetVariableAt(0, 2).Data);
            //Assert.AreEqual(new Integer32(2), result.GetVariableAt(1, 1).Data);
        }

        [Test]
        public void TestGetTable2()
        {
            //IList<Variable> list = new List<Variable>();
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.1.1.1"), new Integer32(12)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.1.1.6"), new Integer32(19)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.1.1.10"), new Integer32(17)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.1.1.12"), new Integer32(76)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.2.1.1"), new Integer32(82)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.2.1.6"), new Integer32(42)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.2.1.10"), new Integer32(13)));
            //list.Add(new Variable(new ObjectIdentifier(".1.3.6.1.2.1.43.11.1.1.2.1.12"), new Integer32(92)));
            //TableCollection result = new TableCollection(new ObjectIdentifier("1.3.6.1.2.1.43.11.1"), list);
            //Assert.AreEqual(2, result.IndexCount);
            //Assert.AreEqual(new Integer32(12), result.GetVariableAt(0, 0, 0).Data);
            //Assert.AreEqual(new Integer32(76), result.GetVariableAt(0, 0, 11).Data);
            //Assert.AreEqual(new Integer32(42), result.GetVariableAt(1, 1, 5).Data);
        }
    }
}
#pragma warning restore 1591