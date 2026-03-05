using System;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Mib;

namespace snmptranslate
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(@"This application takes one parameter.");
                return;
            } 

            string oid = args[0];
            IObjectRegistry registry = new ReloadableObjectRegistry("modules");
            IObjectTree tree = registry.Tree;
            var o = tree.Search(ObjectIdentifier.Convert(oid));
            string textual = o.AlternativeText;
            Console.WriteLine(textual);
            if (o.GetRemaining().Count == 0)
            {
                Console.WriteLine(o.Definition.Type.ToString());
            }
        }
    }
}
