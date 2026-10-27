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
using System.Globalization;
using System.IO;
using System.Xml;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// The RESP listener's per-cache settings, read from <c>resp.ncconf</c>.
    /// <para>
    /// A file of its own, beside <c>config.ncconf</c> in the NCache configuration folder:
    /// </para>
    /// <code>
    /// &lt;configuration&gt;
    ///   &lt;resp-servers&gt;
    ///     &lt;resp-server cache-id="defaultCache" enabled="true" port="9981"
    ///                  max-connections="1000" idle-timeout="300"/&gt;
    ///   &lt;/resp-servers&gt;
    /// &lt;/configuration&gt;
    /// </code>
    /// <para>
    /// Keeping it out of <c>config.ncconf</c> matters for a reason that bit this feature once
    /// already: the NCache service rewrites <c>config.ncconf</c> in full from its in-memory
    /// configuration whenever a cache is registered, applied or unregistered, so a RESP setting
    /// living there could be silently dropped by an unrelated management action. A separate file is
    /// never touched by that path.
    /// </para>
    /// <para>
    /// It also fails closed, in three ways that all mean "do not listen": the file is absent, the
    /// cache has no entry, or the entry is not <c>enabled="true"</c>. Nothing about a cache opens a
    /// port unless this file says so, which is the behaviour an upgrade needs - and the reason a
    /// missing file is logged at detail rather than as an error.
    /// </para>
    /// <para>
    /// The parse is deliberately its own small thing rather than a trip through NCache's
    /// configuration builder. The listener binds its port before the service tells the host to start
    /// the cache, so at that moment nothing has loaded any configuration object yet; reading the XML
    /// directly is what makes the settings available that early.
    /// </para>
    /// </summary>
    internal sealed class RespServerConfig
    {
        private const string Module = "RespServerConfig";

        /// <summary>The file, beside config.ncconf.</summary>
        internal const string FileName = "resp.ncconf";

        public const int DefaultPort = 9981;
        public const int DefaultMaxConnections = 1000;
        public const int DefaultIdleTimeout = 0;

        /// <summary>TCP port to bind. Must be unique per cache on a machine.</summary>
        public int Port { get; private set; }

        /// <summary>
        /// Address to bind. Empty inherits NCacheServer.BindToClientServerIP, the same interface the
        /// protobuf client listener uses. Not in the documented schema, and read when present
        /// because a host with several NICs needs a way to say which one.
        /// </summary>
        public string BindIp { get; private set; }

        /// <summary>Concurrent connections accepted - the Redis "maxclients" analogue.</summary>
        public int MaxConnections { get; private set; }

        /// <summary>Seconds an idle connection is held; 0 disables, like Redis "timeout 0".</summary>
        public int IdleTimeout { get; private set; }

        private RespServerConfig()
        {
            Port = DefaultPort;
            BindIp = string.Empty;
            MaxConnections = DefaultMaxConnections;
            IdleTimeout = DefaultIdleTimeout;
        }

        /// <summary>
        /// The settings for <paramref name="cacheName"/>, or null when this cache must not listen.
        /// Never throws: a malformed file leaves RESP off rather than stopping the cache host.
        /// </summary>
        public static RespServerConfig ForCache(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName)) return null;

            string path = ResolvePath();
            if (path == null) return null;

            if (!File.Exists(path))
            {
                RespLog.Detail(Module + ".ForCache",
                    FileName + " not found at " + path + "; no cache opens a RESP port.");
                return null;
            }

            try
            {
                var document = new XmlDocument();
                document.Load(path);

                XmlNode match = null;
                XmlNodeList entries = document.SelectNodes("/configuration/resp-servers/resp-server");

                if (entries != null)
                {
                    foreach (XmlNode entry in entries)
                    {
                        string id = Attribute(entry, "cache-id");
                        if (!string.IsNullOrEmpty(id) &&
                            id.Trim().Equals(cacheName, StringComparison.OrdinalIgnoreCase))
                        {
                            match = entry;
                            break;   // first entry wins; a duplicate cache-id is a config mistake
                        }
                    }
                }

                if (match == null)
                {
                    RespLog.Detail(Module + ".ForCache",
                        "cache '" + cacheName + "' has no entry in " + path + "; RESP listener not started.");
                    return null;
                }

                // Anything but an explicit true is off, including a missing attribute and a typo.
                // An attribute nobody can read as "on" must never open a port.
                if (!ReadBool(match, "enabled"))
                {
                    RespLog.Detail(Module + ".ForCache",
                        "RESP listener disabled for cache '" + cacheName + "' by enabled=\"" +
                        (Attribute(match, "enabled") ?? string.Empty) + "\" in " + FileName + ".");
                    return null;
                }

                var config = new RespServerConfig
                {
                    Port = ReadInt(match, "port", DefaultPort, cacheName),
                    BindIp = (Attribute(match, "bind-ip") ?? string.Empty).Trim(),
                    MaxConnections = ReadInt(match, "max-connections", DefaultMaxConnections, cacheName),
                    IdleTimeout = ReadInt(match, "idle-timeout", DefaultIdleTimeout, cacheName)
                };

                RespLog.Detail(Module + ".ForCache",
                    "cache '" + cacheName + "': port " + config.Port +
                    ", max-connections " + config.MaxConnections +
                    ", idle-timeout " + config.IdleTimeout +
                    (config.BindIp.Length == 0 ? string.Empty : ", bind-ip " + config.BindIp) +
                    " (from " + path + ")");

                return config;
            }
            catch (Exception e)
            {
                // A broken file leaves the port shut. Reading it as "enabled with defaults" would
                // open a port the operator never described, which is the opposite of what a
                // configuration error should do.
                RespLog.Error(Module + ".ForCache",
                    "could not read " + path + " for cache '" + cacheName + "'; RESP listener not started. " + e);
                return null;
            }
        }

        /// <summary>
        /// Where the file lives: the folder NCache keeps config.ncconf in, with the install folder as
        /// a fallback for the case where the management layer has not scanned the registry yet.
        /// </summary>
        private static string ResolvePath()
        {
            try
            {
                string directory = Alachisoft.NCache.Management.CacheConfigManager.DirName;

                if (string.IsNullOrEmpty(directory))
                {
                    string install = Alachisoft.NCache.Common.AppUtil.InstallDir;
                    if (string.IsNullOrEmpty(install))
                    {
                        RespLog.Error(Module + ".ResolvePath",
                            "no NCache configuration folder could be located; RESP listener not started.");
                        return null;
                    }
                    directory = Path.Combine(install, "config");
                }

                return Path.Combine(directory, FileName);
            }
            catch (Exception e)
            {
                RespLog.Error(Module + ".ResolvePath", "could not locate " + FileName + ". " + e);
                return null;
            }
        }

        private static string Attribute(XmlNode node, string name)
        {
            if (node == null || node.Attributes == null) return null;
            XmlAttribute attribute = node.Attributes[name];
            return attribute == null ? null : attribute.Value;
        }

        private static bool ReadBool(XmlNode node, string name)
        {
            string raw = Attribute(node, name);
            if (string.IsNullOrEmpty(raw)) return false;

            raw = raw.Trim();
            return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1";
        }

        /// <summary>
        /// An integer attribute, falling back to the default when absent or unreadable. A bad value
        /// is logged rather than silently taken as zero - an idle-timeout of 0 means "never close"
        /// and a port of 0 would be refused later, so a typo must not look like a deliberate choice.
        /// </summary>
        private static int ReadInt(XmlNode node, string name, int fallback, string cacheName)
        {
            string raw = Attribute(node, name);
            if (string.IsNullOrEmpty(raw)) return fallback;

            int value;
            if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;

            RespLog.Warn(Module + ".ReadInt",
                "cache '" + cacheName + "': " + name + "=\"" + raw + "\" in " + FileName +
                " is not a number; using " + fallback + ".");
            return fallback;
        }
    }
}
