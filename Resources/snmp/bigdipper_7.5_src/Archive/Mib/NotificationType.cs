using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal sealed class NotificationType : IEntity
    {
        private readonly string _module;
        private string _parent;
        private readonly uint _value;
        private readonly string _name;

        public NotificationType(string module, IList<Symbol> header, Lexer lexer)
        {
            _module = module;
            _name = header[0].ToString();
            lexer.ParseOidValue(out _parent, out _value);
        }

        public string ModuleName
        {
            get { return _module; }
        }

        public string Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public uint Value
        {
            get { return _value; }
        }

        public string Name
        {
            get { return _name; }
        }
        
        public string Description
        {
            // TODO: implement this.
            get { return string.Empty; }
        }
    }
}