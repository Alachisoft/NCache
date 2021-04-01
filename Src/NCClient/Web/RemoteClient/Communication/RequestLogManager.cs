//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.Client
{
    internal class RequestLogManager : IDisposable
    {
        Dictionary<string, RequestLogger> loggerDictionary;

        public RequestLogManager()
        {
            loggerDictionary = new Dictionary<string, RequestLogger>();
        }

        public RequestLogger GetLogger(string ipAddress, bool createIfNotFound)
        {
            RequestLogger logger = null;
            lock (this)
            {
                bool found = loggerDictionary.TryGetValue(ipAddress, out logger);
                if (!found && createIfNotFound)
                {
                    logger = new RequestLogger();
                    loggerDictionary.Add(ipAddress, logger);
                }
            }
            return logger;
        }

        /// <summary>
        /// Gets the collection of RequestLoggers
        /// </summary>
        public ICollection<RequestLogger> RequestLoggers
        {
            get { return loggerDictionary.Values; }
        }

        public void Dispose()
        {
            loggerDictionary = null;
        }

        internal void Expire(int expirationInterval)
        {
            ICollection<string> ipAddressList = null;
            lock (this)
            {
                ipAddressList = loggerDictionary.Keys;
            }

            foreach (string ipAddress in ipAddressList)
            {
                RequestLogger logger = null;
                bool found = loggerDictionary.TryGetValue(ipAddress, out logger);

                if (found)
                    logger.Expire(expirationInterval);
            }
        }

        public void RemoveRequest(long requestId)
        {
            lock (loggerDictionary)
            {
                foreach (RequestLogger logger in loggerDictionary.Values)
                {
                    logger.RemoveRequest(requestId);
                }
            }
        }
    }
}
