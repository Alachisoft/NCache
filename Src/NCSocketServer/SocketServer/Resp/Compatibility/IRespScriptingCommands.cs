//  Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// The scripting and functions commands - 8 of them, on 5 members: all 5 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespScriptingCommands
    {
        /// <summary>EVAL and EVAL_RO.</summary>
        [RespCommand("EVAL", "EVAL_RO")]
        RespReply Eval(byte[] script, IList<string> keys, IList<byte[]> arguments, bool readOnly);

        /// <summary>EVALSHA and EVALSHA_RO.</summary>
        [RespCommand("EVALSHA", "EVALSHA_RO")]
        RespReply EvalSha(string sha1, IList<string> keys, IList<byte[]> arguments, bool readOnly);

        /// <summary>SCRIPT, a container command.</summary>
        [RespCommand("SCRIPT")]
        RespReply Script(RespArgs args);

        /// <summary>FUNCTION, a container command.</summary>
        [RespCommand("FUNCTION")]
        RespReply Function(RespArgs args);

        /// <summary>FCALL and FCALL_RO.</summary>
        [RespCommand("FCALL", "FCALL_RO")]
        RespReply FunctionCall(string name, IList<string> keys, IList<byte[]> arguments, bool readOnly);
    }
}
