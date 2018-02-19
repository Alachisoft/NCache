// Copyright (c) 2018 Alachisoft
// 
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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Runtime.Processor
{
    /// <summary>
    /// Holds the exceptions for EntryProcessor.
    /// </summary>
    [Serializable]
    public class EntryProcessorException : Exception
    {
        public EntryProcessorException()
        { }
        /// <summary>
        /// Initialize given message as an exception.
        /// </summary>
        /// <param name="message">Exception message</param>
        public EntryProcessorException(string message)
            : base(message)
        { }
        /// <summary>
        /// Initialize an instance of Exception with seralized information.
        /// </summary>
        /// <param name="info">Seralized data for which exception is being thrown.</param>
        /// <param name="context">Contextual information about source or destination. </param>

        public EntryProcessorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
        /// <summary>
        /// Initialize inner exception with given explainatory message.
        /// </summary>
        /// <param name="message">Message that explains inner exception. </param>
        /// <param name="innerException">Inner exception thrown i.e NUllRefrenceException</param>
        public EntryProcessorException(string message, Exception innerException)
            : base(message, innerException)
        { }
        /// <summary>
        ///Initialize  exception and cancatinates it with the relevant message. 
        /// </summary>
        /// <param name="exception">Inner or external exception. </param>
        public EntryProcessorException(Exception exception)
            : base(exception.Message, exception)
        { }
    }
}
