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
// limitations under the License.

using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;

namespace Alachisoft.NCache.SignalR
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver UseNCache(this IDependencyResolver resolver, string cacheName, string eventKey)
        {            
            var configuration = new NCacheScaleoutConfiguration(cacheName, eventKey);


            return UseNCache(resolver, configuration);
        }

        public static IDependencyResolver UseNCache(this IDependencyResolver resolver, NCacheScaleoutConfiguration configuration)
        {
            var bus = new Lazy<NCacheMessageBus>(() => new NCacheMessageBus(resolver, configuration, new NCacheProvider()));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
