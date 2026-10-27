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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// Thrown by a command this build cannot serve, carrying the engine capability it would need.
    /// <para>
    /// The reason text is not written here. It comes from <see cref="RespUnsupported"/>, which
    /// already holds one reason per family and is the table the handbook's command matrix is
    /// generated from - so a capability gap is described once and every layer that reports it says
    /// the same thing. A command refused by the interface and the same command refused by the
    /// dispatcher produce identical wording, because they read the same row.
    /// </para>
    /// <para>
    /// This is deliberately not <see cref="NotSupportedException"/>. The dispatcher has to tell a
    /// refusal it expected from a defect it did not, and a bare NotSupportedException thrown
    /// somewhere inside the engine would be indistinguishable from a command boundary.
    /// </para>
    /// </summary>
    [Serializable]
    internal sealed class RespNotSupportedException : Exception
    {
        public RespNotSupportedException(string command, string reason)
            : base("unsupported command '" + command + "': " + reason)
        {
            Command = command;
            Reason = reason;
        }

        /// <summary>The command that was refused, uppercase.</summary>
        public string Command { get; }

        /// <summary>The reason it was refused, as <see cref="RespUnsupported"/> states it.</summary>
        public string Reason { get; }
    }

    /// <summary>Builds the refusal a default interface member throws.</summary>
    internal static class RespNotSupported
    {
        /// <summary>
        /// The exception for a command the tables refuse. A command not in the refusal table still
        /// throws - an interface member that defaults to throwing is by definition unimplemented -
        /// with the same wording.
        /// </summary>
        public static RespNotSupportedException For(string command)
        {
            return new RespNotSupportedException(command,
                RespUnsupported.ReasonFor(command) ?? RespUnsupported.NotInNCache);
        }
    }
}
