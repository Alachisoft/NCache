using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring.CustomEventEntryLogging
{
    public class CustomEventEntryParser
    {
        public static long filePointerPosition = 0;
        public static DateTime timeStampAtEventRegistration = DateTime.Now;
        public static Queue<CustomEventEntry> customEventEntries = new Queue<CustomEventEntry>();      


        public static void ParseEventLog(string filePath)
        {
            try
            {
                FileStream logFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                using (var reader = new StreamReader(logFile))
                {
                    reader.BaseStream.Seek(filePointerPosition, SeekOrigin.Begin);

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        CustomEventEntry customEventEntry = ParseLine(line);

                        if (customEventEntry != null && customEventEntry.TimeStamp > timeStampAtEventRegistration)
                            customEventEntries.Enqueue(customEventEntry);
                    }
                    filePointerPosition = reader.BaseStream.Position;
                }
            }
            catch (Exception e) { AppUtil.LogEvent("NCacheEventLog", e.ToString(), EventLogEntryType.Error, -1, -1); }
        }

        private static CustomEventEntry ParseLine(string line)
        {
            try
            {
                string[] lineParts = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);

                if (lineParts.Length > 4)
                {
                    return new CustomEventEntry()
                    {
                        TimeStamp = DateTime.ParseExact(lineParts[0].Trim(), "yyyy-MM-dd HH:mm:ss, fff", CultureInfo.InvariantCulture),
                        Source = lineParts[1].Trim(),
                        EventId = Convert.ToInt64(lineParts[2].Trim()),
                        Level = CustomLogEntryType(lineParts[3].Trim()),
                        Message = lineParts[4].Trim(),
                    };
                }
                else if (lineParts.Length == 1)
                {
                    if (customEventEntries.Count != 0)
                        customEventEntries.Peek().Message += Environment.NewLine + lineParts[0];
                }
            }
            catch { }

            return null;
        }

        #region Util Methods
        private static EventLogEntryType CustomLogEntryType(string level)
        {
            switch (level)
            {
                case ("Error"):
                    return EventLogEntryType.Error;
                case ("Warning"):
                    return EventLogEntryType.Warning;
                case ("Information"):
                    return EventLogEntryType.Information;
                case ("SuccessAudit"):
                    return EventLogEntryType.SuccessAudit;
                case ("FailureAudit"):
                    return EventLogEntryType.FailureAudit;
                default:
                    throw new Exception("CustomLogEntryType(string level): Level is an invalid string");
            }
        }
        #endregion
    }
}
