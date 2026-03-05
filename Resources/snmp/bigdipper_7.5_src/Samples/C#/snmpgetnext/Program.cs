/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/11
 * Time: 12:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Mono.Options;
using Lextm.SharpSnmpLib.Security;
using Lextm.SharpSnmpLib.Messaging;

namespace SnmpGetNext
{ 
    internal static class Program
    {
        public static void Main(string[] args)
        {
            string community = "public";
            bool showHelp   = false;
            bool showVersion = false;
            VersionCode version = VersionCode.V1;
            int timeout = 1000;
            int retry = 0;
            Levels level = Levels.Reportable;
            string user = string.Empty;
            string authentication = string.Empty;
            string authPhrase = string.Empty;
            string privacy = string.Empty;
            string privPhrase = string.Empty;

            OptionSet p = new OptionSet()
                .Add("c:", "Community name, (default is public)", delegate (string v) { if (v != null) community = v; })
                .Add("l:", "Security level, (default is noAuthNoPriv)", delegate(string v)
                                                                                   {
                                                                                       if (v.ToUpperInvariant() == "NOAUTHNOPRIV")
                                                                                       {
                                                                                           level = Levels.Reportable;
                                                                                       }
                                                                                       else if (v.ToUpperInvariant() == "AUTHNOPRIV")
                                                                                       {
                                                                                           level = Levels.Authentication | Levels.Reportable;
                                                                                       }
                                                                                       else if (v.ToUpperInvariant() == "AUTHPRIV")
                                                                                       {
                                                                                           level = Levels.Authentication | Levels.Privacy | Levels.Reportable;
                                                                                       }
                                                                                       else
                                                                                       {
                                                                                           throw new ArgumentException("no such security mode: " + v);
                                                                                       }
                                                                                   })
                .Add("a:", "Authentication method (MD5 or SHA)", delegate(string v) { authentication = v; })
                .Add("A:", "Authentication passphrase", delegate(string v) { authPhrase = v; })
                .Add("x:", "Privacy method", delegate(string v) { privacy = v; })
                .Add("X:", "Privacy passphrase", delegate(string v) { privPhrase = v; })
                .Add("u:", "Security name", delegate(string v) { user = v; })
                .Add("h|?|help", "Print this help information.", delegate(string v) { showHelp = v != null; })
                .Add("V", "Display version number of this application.", delegate (string v) { showVersion = v != null; })
                .Add("t:", "Timeout value (unit is second).", delegate (string v) { timeout = int.Parse(v) * 1000; })
                .Add("r:", "Retry count (default is 0)", delegate (string v) { retry = int.Parse(v); })
                .Add("v:", "SNMP version (1, 2, and 3 are currently supported)", delegate (string v)
                                                                                       {
                                                                                           switch (int.Parse(v))
                                                                                           {
                                                                                               case 1:
                                                                                                   version = VersionCode.V1;
                                                                                                   break;
                                                                                               case 2:
                                                                                                   version = VersionCode.V2;
                                                                                                   break;
                                                                                               case 3:
                                                                                                   version = VersionCode.V3;
                                                                                                   break;
                                                                                               default:
                                                                                                   throw new ArgumentException("no such version: " + v);
                                                                                           }
                                                                                       });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (extra.Count < 2)
            {
                Console.WriteLine("invalid variable number: " + extra.Count);
                return;
            } 
            
            if (showVersion)
            {
                Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                return;
            }
           
            IPAddress ip;
            bool parsed = IPAddress.TryParse(extra[0], out ip);
            if (!parsed)
            {
                foreach (IPAddress address in
                    Dns.GetHostAddresses(extra[0]).Where(address => address.AddressFamily == AddressFamily.InterNetwork))
                {
                    ip = address;
                    break;
                }

                if (ip == null)
                {
                    Console.WriteLine("invalid host or wrong IP address found: " + extra[0]);
                    return;
                }
            }

            try
            {
                List<Variable> vList = new List<Variable>();
                for (int i = 1; i < extra.Count; i++)
                {
                    Variable test = new Variable(new ObjectIdentifier(extra[i]));
                    vList.Add(test);
                }

                IPEndPoint receiver = new IPEndPoint(ip, 161);
                if (version != VersionCode.V3)
                {
                    GetNextRequestMessage message = new GetNextRequestMessage(0,
                                                                              version,
                                                                              new OctetString(community),
                                                                              vList);
                    ISnmpMessage response = message.GetResponse(timeout, receiver);
                    if (response.Pdu().ErrorStatus.ToInt32() != 0) // != ErrorCode.NoError
                    {
                        throw ErrorException.Create(
                            "error in response",
                            receiver.Address,
                            response);
                    }

                    foreach (Variable variable in response.Pdu().Variables)
                    {
                        Console.WriteLine(variable);
                    }

                    return;
                }

                if (string.IsNullOrEmpty(user))
                {
                    Console.WriteLine("User name need to be specified for v3.");
                    return;
                }

                IAuthenticationProvider auth = (level & Levels.Authentication) == Levels.Authentication
                                                   ? GetAuthenticationProviderByName(authentication, authPhrase)
                                                   : DefaultAuthenticationProvider.Instance;

                IPrivacyProvider priv;
                if ((level & Levels.Privacy) == Levels.Privacy)
                {
                    priv = new DESPrivacyProvider(new OctetString(privPhrase), auth);
                }
                else
                {
                    priv = new DefaultPrivacyProvider(auth);
                }

                Discovery discovery = Messenger.NextDiscovery;
                ReportMessage report = discovery.GetResponse(timeout, receiver);

                GetNextRequestMessage request = new GetNextRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(user), vList, priv, Messenger.MaxMessageSize, report);

                ISnmpMessage reply = request.GetResponse(timeout, receiver);
                if (reply.Pdu().ErrorStatus.ToInt32() != 0) // != ErrorCode.NoError
                {
                    throw ErrorException.Create(
                        "error in response",
                        receiver.Address,
                        reply);
                }

                foreach (Variable v in reply.Pdu().Variables)
                {
                    Console.WriteLine(v);
                }
            }
            catch (SnmpException ex)
            {
                Console.WriteLine(ex);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static IAuthenticationProvider GetAuthenticationProviderByName(string authentication, string phrase)
        {
            if (authentication.ToUpperInvariant() == "MD5")
            {
                return new MD5AuthenticationProvider(new OctetString(phrase));
            }

            if (authentication.ToUpperInvariant() == "SHA")
            {
                return new SHA1AuthenticationProvider(new OctetString(phrase));
            }

            throw new ArgumentException("unknown name", "authentication");
        }
        
        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("#SNMP is available at http://sharpsnmplib.codeplex.com");
            Console.WriteLine("snmpgetnext [Options] IP-address|host-name OID [OID] ...");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}