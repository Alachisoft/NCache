using System;
using System.Net.Sockets;

namespace Alachisoft.NGroups.Blocks
{
    /// <summary>
    /// Customized error messages for socket opertions failure.
    /// </summary>
    public class ExtSocketException : SocketException
    {
        // local message property
        string message;

        public ExtSocketException(String message)
        {
            this.message = message;
        }
        /// <summary>
        /// Gets the error message for the exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return message;
            }
        }
    }
}