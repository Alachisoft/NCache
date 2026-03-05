/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/23
 * Time: 19:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Pipeline;
using Lextm.SharpSnmpLib.Security;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;

namespace SnmpTrapD
{
    internal static class Program
    { 
        internal static IUnityContainer Container { get; private set; }
        
        public static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }
            
            Container = new UnityContainer().LoadConfiguration("snmptrapd");
            var users = Container.Resolve<UserRegistry>();
            users.Add(new OctetString("neither"), DefaultPrivacyProvider.DefaultPair);
            users.Add(new OctetString("authen"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication"))));
            users.Add(new OctetString("privacy"), new DESPrivacyProvider(new OctetString("privacyphrase"),
                                                                         new MD5AuthenticationProvider(new OctetString("authentication")))); 

            var trapv1 = Container.Resolve<TrapV1MessageHandler>("TrapV1Handler");
            trapv1.MessageReceived += WatcherTrapV1Received;
            var trapv2 = Container.Resolve<TrapV2MessageHandler>("TrapV2Handler");
            trapv2.MessageReceived += WatcherTrapV2Received;
            var inform = Container.Resolve<InformRequestMessageHandler>("InformHandler");
            inform.MessageReceived += WatcherInformRequestReceived;
            using (var engine = Container.Resolve<SnmpEngine>())
            {
                engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, 162));
                engine.Start();
                Console.WriteLine("#SNMP is available at http://sharpsnmplib.codeplex.com");
                Console.WriteLine("Press any key to stop . . . ");
                Console.Read();
                engine.Stop();
            }
        }

        private static void WatcherInformRequestReceived(object sender, InformRequestMessageReceivedEventArgs e)
        {
            Console.WriteLine(e.InformRequestMessage);
        }

        private static void WatcherTrapV2Received(object sender, TrapV2MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.TrapV2Message);
        }

        private static void WatcherTrapV1Received(object sender, TrapV1MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.TrapV1Message);
        }
    }
}