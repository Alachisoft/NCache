namespace Lextm.SharpSnmpLib.Mib
{
    internal class CustomType : ITypeAssignment
    {
        public CustomType(string module, string name, Lexer lexer)
        {
            Module = module;
            Name = name;
            Symbol previous = null;
            Symbol temp;
            while ((temp = lexer.GetNextSymbol()) != null)
            {
                if (previous == Symbol.EOL && temp == Symbol.EOL)
                {
                    return;
                }

                previous = temp;
            }

            if (previous != null)
            {
                previous.Throw("end of file reached");
            }
        }

        public string Module { get; set; }
        public string Name { get; private set; }
    }
}