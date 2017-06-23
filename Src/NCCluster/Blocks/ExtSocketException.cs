// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
