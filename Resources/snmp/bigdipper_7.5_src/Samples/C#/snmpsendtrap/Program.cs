/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/7/9
 * Time: 21:09
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Net;

using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace SnmpSendTrap
{
    internal static class Program 
    {
        public static void Main(string[] args)
        {
            IPAddress address = args.Length == 1 ? IPAddress.Parse(args[0]) : IPAddress.Loopback;

            Messenger.SendTrapV1(new IPEndPoint(address, 162), IPAddress.Loopback,
                                 new OctetString("public"),
                                 new ObjectIdentifier(new uint[] { 1, 3, 6 }),
                                 GenericCode.ColdStart,
                                 0,
                                 0,
                                 new List<Variable>());

            //Thread.Sleep(50);


            Messenger.SendTrapV2(0, VersionCode.V2, new IPEndPoint(address, 162), 
                                 new OctetString("public"),
                                 new ObjectIdentifier(new uint[] { 1, 3, 6 }),
                                 0,
                                 new List<Variable>());
            //Thread.Sleep(50);

            try
            {
                Messenger.SendInform(0, VersionCode.V2, new IPEndPoint(address, 162), new OctetString("public"), new ObjectIdentifier(new uint[] { 1, 3, 6 }),
                                     0,
                                     new List<Variable>(), 2000, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }
    }
}