/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 16:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Globalization;
#if (!SILVERLIGHT)
using System.Runtime.Serialization;
using System.Security.Permissions; 
#endif

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Description of MibException.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mib")]
    public sealed class MibException : SnmpException
    {
        /// <summary>
        /// Symbol.
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Creates a <see cref="MibException"/>.
        /// </summary>
        public MibException()
        {
        }
        
        /// <summary>
        /// Creates a <see cref="SnmpException"/> instance with a specific <see cref="string"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public MibException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Creates a <see cref="MibException"/> instance with a specific <see cref="string"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public MibException(string message, Exception inner)
            : base(message, inner)
        {
        }
#if (!SILVERLIGHT)        
        /// <summary>
        /// Creates a <see cref="MibException"/> instance.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        private MibException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            
            Symbol = (Symbol)info.GetValue("Symbol", typeof(Symbol));
        }
        
        /// <summary>
        /// Gets object data.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Symbol", Symbol);
        }
#endif        
        /// <summary>
        /// Details on error.
        /// </summary>
        protected override string Details
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "wrong symbol {0} in file \"{1}\". Row {2}. Column: {3}",
                    Symbol,
                    Symbol.File,
                    (Symbol.Row + 1).ToString(CultureInfo.InvariantCulture),
                    (Symbol.Column + 1).ToString(CultureInfo.InvariantCulture));
            }
        }
        
        /// <summary>
        /// Creates a <see cref="MibException"/> with a specific <see cref="Symbol"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public static MibException Create(string message, Symbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            MibException ex = new MibException(message + ". Wrong entity, " + symbol + " in file \"" + symbol.File + "\". row: " + (symbol.Row + 1).ToString(CultureInfo.InvariantCulture) + "; column: " + (symbol.Column + 1).ToString(CultureInfo.InvariantCulture)) { Symbol = symbol };
            return ex;
        }
    }
}