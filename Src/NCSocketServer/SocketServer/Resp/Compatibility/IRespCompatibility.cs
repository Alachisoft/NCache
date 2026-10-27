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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// Every Redis command this listener knows about, as one interface: 245 commands on 185
    /// members, across 17 families.
    /// <para>
    /// This is the seam the four-layer model calls for. Above it sits the dispatcher, which
    /// parses a request and renders a reply and knows nothing about a cache; below it sits a
    /// provider, which serves commands and knows nothing about the wire. Nothing in this
    /// interface or the types it uses mentions <c>RespRequest</c>, <c>RespWriter</c> or
    /// <c>Alachisoft.NCache.Caching</c> - which is the whole of what it is for, and is checked
    /// rather than asserted.
    /// </para>
    /// <para>
    /// A command NCache cannot serve is not absent from here. It is declared, and throws
    /// <see cref="RespNotSupportedException"/> from a default implementation, with the reason
    /// <see cref="RespUnsupported"/> gives. So an implementer writes nothing at all for the
    /// commands this build refuses, and the compiler still names every command it must supply.
    /// When a capability gap closes, deleting the default body turns that command into a
    /// compiler error at each provider - which is the point.
    /// </para>
    /// <para>
    /// Completeness is a test, not a claim: every command in <see cref="RespCommandTable"/> and
    /// every command in <see cref="RespUnsupported"/> must appear on exactly one member, by way
    /// of <see cref="RespCommandAttribute"/>.
    /// </para>
    /// </summary>
    internal interface IRespCompatibility :
        IRespConnectionCommands,
        IRespServerCommands,
        IRespClusterCommands,
        IRespAclCommands,
        IRespKeyCommands,
        IRespStringCommands,
        IRespBitmapCommands,
        IRespHashCommands,
        IRespListCommands,
        IRespSetCommands,
        IRespSortedSetCommands,
        IRespStreamCommands,
        IRespPubSubCommands,
        IRespTransactionCommands,
        IRespScriptingCommands,
        IRespHyperLogLogCommands,
        IRespGeoCommands
    {
    }
}
