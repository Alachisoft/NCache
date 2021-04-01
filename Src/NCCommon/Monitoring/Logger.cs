//  Copyright (c) 2021 Alachisoft
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
using System.Text;
using System.Collections;
using System.IO;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class Logger
    {
        private TextWriter _writer;
        const string STERIC = "*****************************************************************************************************";
        public const string TIME_FORMAT = "dd-MM-yy HH:mm:ss:ffff";

        private object _sync_mutex = new object();

        /// <summary>
        /// True if writer is not instantiated, false otherwise
        /// </summary>
        public bool IsWriterNull
        {
            get
            {
                if (_writer == null) return true;
                else return false;
            }
        }

        /// <summary>
        /// Creates logs in installation folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string directory)
        {
            Initialize(fileName, null, directory);
        }

        /// <summary>
        /// Creates logs in provided folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="filepath">path where logs are to be created</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string filepath, string directory)
        {
            lock (_sync_mutex)
            {
                string filename = fileName + "." +
                    Environment.MachineName.ToLower() + "." +
                    DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".logs.txt";

                if (filepath == null || filepath == string.Empty)
                {

                    if (!DirectoryUtil.SearchGlobalDirectory("log-files", false, out filepath))
                    {
                        try
                        {
                            DirectoryUtil.SearchLocalDirectory("log-files", true, out filepath);

                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Unable to initialize the log file", ex);
                        }
                    }
                }

                try
                {
                    filepath = Path.Combine(filepath, directory);
                    if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);

                    filepath = Path.Combine(filepath, filename);

                    _writer = TextWriter.Synchronized(new StreamWriter(filepath, false));
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public void WriteClientActivities(Hashtable activityTable,bool completed)
        {
            if (activityTable != null)
            {
                try
                {
                    string status = completed ? "completed" : "in_execution";
                    WriteSingleLine("CLIENT ACTIVITY LOG [clients :" + activityTable.Count + " status : " + status + "]"); 
                    IDictionaryEnumerator ide = activityTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        ClienfInfo cInfo = ide.Key as ClienfInfo;
                        ArrayList activities = ide.Value as ArrayList;

                        if (cInfo != null)
                        {
                            WriteSingleLine(STERIC);
                            WriteSingleLine("client [id :" + cInfo.ID + " address :" + cInfo.Address + "]");
                            WriteSingleLine(STERIC);

                        }

                        if (activities != null && activities.Count > 0)
                        {
                            foreach (ClientActivity cActivity in activities)
                            {
                                WriteClientActivity(cActivity);
                            }
                        }
                        else
                        {
                            WriteSingleLine("No activity observered");
                        }

                        WriteSingleLine("activity for client_id :" + cInfo.ID + " ends here");
                        WriteSingleLine("");
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        public void WriteSingleLine(string logText)
        {
            if (_writer != null)
            {
                lock (_sync_mutex)
                {
                    _writer.WriteLine(logText);
                    _writer.Flush();
                }
            }
        }
        public void WriteClientActivity(ClientActivity activity)
        {
            if (_writer != null && activity != null)
            {
                TimeSpan tspan = activity.EndTime - activity.StartTime;

                WriteSingleLine("activity [duration(ms) :" +tspan.TotalMilliseconds +  " start_time :" + activity.StartTime.ToString(TIME_FORMAT) + " end_time :" + activity.EndTime.ToString(TIME_FORMAT) + "]");

                if (activity.Activities != null)
                {
                    for (int i = 0; i < activity.Activities.Count; i++)
                    {
                        MethodActivity mActivity = activity.Activities[i] as MethodActivity;
                        if (mActivity != null)
                        {
                            int space2 = 40;
                            string line = null;
                            line = mActivity.Time.ToString(TIME_FORMAT) + ":  " + mActivity.MethodName.PadRight(space2, ' ') + mActivity.Log;
                            lock (_sync_mutex)
                            {
                                _writer.WriteLine(line);
                                _writer.Flush();
                            }
                        }
                    }
                }
                WriteSingleLine("activity ends");
                WriteSingleLine("");

            }
        }
        /// <summary>
        /// Close writer
        /// </summary>
        public void Close()
        {
            lock (_sync_mutex)
            {
                if (_writer != null)
                {
                    lock (_writer)
                    {
                        _writer.Close();
                        _writer = null;
                    }
                }
            }
        }
    }
}
