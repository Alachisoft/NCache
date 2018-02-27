﻿// Copyright (c) 2018 Alachisoft
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
using System.Security.Cryptography;
using Alachisoft.NCache.Web.SessionState.Interface;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class SessionKeyGenerator : ISessionKeyGenerator
    {

        RandomNumberGenerator _randomGenerator;
        public SessionKeyGenerator()
        {

            _randomGenerator = RandomNumberGenerator.Create();
        }
        public string Create()
        {
            var bytes = new byte[16];
            _randomGenerator.GetBytes(bytes);
            return new Guid(bytes).ToString();
        }
    }
}
