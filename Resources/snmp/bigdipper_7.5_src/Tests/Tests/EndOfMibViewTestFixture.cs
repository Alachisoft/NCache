/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/2/15
 * Time: 20:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
#pragma warning disable 1591, 0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
	/// <summary>
	/// Description of TestEndOfMibView.
	/// </summary>
	[TestFixture]
	public class EndOfMibViewTestFixture
	{
		[Test]
		public void TestToBytes()
		{
			EndOfMibView obj = new EndOfMibView();
			Assert.AreEqual(new byte[] { 0x82, 0x00 }, obj.ToBytes());
            Assert.AreEqual(0, obj.GetHashCode());

            EndOfMibView right = new EndOfMibView();
            Assert.AreEqual(obj, right);
            Assert.IsTrue(obj.Equals(right));
            Assert.IsTrue(obj != null);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(obj == obj);
// ReSharper restore EqualExpressionComparison

		    Assert.Throws<ArgumentNullException>(() => obj.AppendBytesTo(null));
            Assert.AreEqual("EndOfMibView", obj.ToString());
		}
	}
}
#pragma warning restore 1591, 0618,1718