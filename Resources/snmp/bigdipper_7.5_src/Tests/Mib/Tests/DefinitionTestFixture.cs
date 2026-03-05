/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/9/30
 * Time: 11:04
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Mib.Tests
{
    [TestFixture]
    public class DefinitionTestFixture
    {
        [Test]
        public void TestGetAlternativeTextualForm()
        {            
            Definition root = Definition.RootDefinition;
            Definition iso = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "iso", null, 1), root);
            Definition org = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "org", "iso", 3), iso);
            Definition dod = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "dod", "org", 6), org);
            Definition internet = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "internet", "dod", 1), dod);
            Definition mgmt = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "mgmt", "internet", 2), internet);
            Definition mib2 = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "mib-2", "mgmt", 1), mgmt);
            Definition system = new Definition(new ObjectIdentifierType("SNMPV2-SMI", "system", "mib-2", 1), mib2);
            Assert.AreEqual(".iso.org.dod.internet.mgmt.mib-2.system",
                            new SearchResult(system, new uint[0]).AlternativeText);
        }
    }
}
