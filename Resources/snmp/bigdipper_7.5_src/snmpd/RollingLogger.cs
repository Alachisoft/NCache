/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 3/14/2010
 * Time: 1:18 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Pipeline;

namespace Lextm.SharpSnmpLib.Agent
{
    /// <summary>
    /// Logger class, who logs message processed to the rolling log file.
    /// </summary>    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class RollingLogger : ILogger
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string Empty = "-";
        
        public RollingLogger()
        {
            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            Logger.Info(string.Format(CultureInfo.InvariantCulture, "#Software: #SNMP Agent {0}", System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
            Logger.Info("#Version: 1.0");
            Logger.Info(string.Format(CultureInfo.InvariantCulture, "#Date: {0}", DateTime.UtcNow));
            Logger.Info("#Fields: date time s-ip cs-method cs-uri-stem s-port cs-username c-ip sc-status cs-version time-taken");
        }

        public void Log(ISnmpContext context)
        {
            if (Logger.IsInfoEnabled)
            {
                Logger.Info(GetLogEntry(context));
            }
        }

        private static string GetLogEntry(ISnmpContext context)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                DateTime.UtcNow,
                context.Binding.Endpoint.Address,
                context.Request.TypeCode() == SnmpType.Unknown ? Empty : context.Request.TypeCode().ToString(),
                GetStem(context.Request.Pdu().Variables),
                context.Binding.Endpoint.Port,
                context.Request.Parameters.UserName,
                context.Sender.Address,
                (context.Response == null) ? Empty : context.Response.Pdu().ErrorStatus.ToErrorCode().ToString(),
                context.Request.Version,
                DateTime.Now.Subtract(context.CreatedTime).TotalMilliseconds);
        }

        private static string GetStem(ICollection<Variable> variables)
        {
            if (variables.Count == 0)
            {
                return Empty;
            }

            StringBuilder result = new StringBuilder();
            foreach (Variable v in variables)
            {
                result.AppendFormat("{0};", v.Id);
            }

            if (result.Length > 0)
            {
                result.Length--;
            }

            return result.ToString();
        }
    }
}
