/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/6/2010
 * Time: 3:26 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System.Net;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Messaging.Tests
{
    /// <summary>
    /// Description of ListenerBindingTestFixture.
    /// </summary>
    [TestFixture]
    public class ListenerTestFixture
    {
        [Test]
        public void AddBindingDuplicate()
        {
            Assert.AreEqual(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            var listener = new Listener();
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            Assert.AreEqual(1, listener.Bindings.Count);
        }
        
        [Test]
        public void RemoveBinding()
        {
            Assert.AreEqual(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            var listener = new Listener();
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            listener.RemoveBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            Assert.AreEqual(0, listener.Bindings.Count);
        }
    }
}
