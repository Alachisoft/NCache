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
    /// The Redis command names a member of <see cref="IRespCompatibility"/> serves.
    /// <para>
    /// This is what makes "the interface is complete" a checkable statement rather than a claim.
    /// Every command the listener knows about - the 99 in <see cref="RespCommandTable"/> and the
    /// refusals in <see cref="RespUnsupported"/> - must appear on exactly one member, and the
    /// conformance test fails when one does not. Adding a command to either of those tables without
    /// declaring it here is therefore a build-visible omission rather than a silent gap.
    /// </para>
    /// <para>
    /// More than one name per member is normal where Redis has aliases or near-aliases that differ
    /// only in a flag the caller passes: GETRANGE and SUBSTR are the same operation, and HGETALL,
    /// HKEYS and HVALS differ only in which halves of the pair they return.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class RespCommandAttribute : Attribute
    {
        public RespCommandAttribute(params string[] names) { Names = names; }

        /// <summary>The command names, uppercase, as they arrive on the wire.</summary>
        public string[] Names { get; }
    }
}
