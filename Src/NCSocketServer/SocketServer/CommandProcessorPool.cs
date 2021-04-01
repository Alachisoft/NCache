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

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    internal class CommandProcessorPool
    {
        private readonly int _maxProcessors;
        private readonly CommandProcessor[] _workers;

        public CommandProcessorPool(int processors, IRequestProcessor reqProcessor, StatisticsCounter collector)
        {
            _maxProcessors = processors;
            _workers = new CommandProcessor[processors];

            for (int i = 0; i < processors; i++){
                _workers[i] = new CommandProcessor(reqProcessor, collector);
            }
        }

        public void EnqueuRequest(ProcCommand request, long indexFeed)
        {
            _workers[indexFeed % _maxProcessors].EnqueuRequest(request);
        }

        public void Start()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Start();
                }
            }
        }

        public void Stop()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Stop();
                }
            }
        }
    }
}
