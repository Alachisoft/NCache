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

namespace Alachisoft.NCache.Web.Caching.APILogging
{
    internal class DebugAPIConfiguraions
    {
        private static int s_timeBeforeLoggingStart = 0;
        private static int s_numberOfIterations = 1;
        private static int s_durationOfEachIteration = 0;
        private static int s_intervalBetweenIterations = 0;
        private static bool s_loggingEnabled = false;
        private static int s_loggerThreadLoggingInterval = 5;

        /// <summary>
        /// Indicates that all logging intervals has been passed.
        /// </summary>
        private bool _loggingExpired = false;
        private DateTime _loggingStartTime;

        static DebugAPIConfiguraions()
        {
            loadConfiguration();
        }

        public DebugAPIConfiguraions()
        {
            _loggingStartTime = DateTime.Now.AddSeconds(s_timeBeforeLoggingStart);
        }

        /// <summary>
        /// Gets time in seconds after cache start, after which logging should be started
        /// </summary>
        public static int TimeBeforeLoggingStart
        {
            get { return s_timeBeforeLoggingStart; }
            set { s_timeBeforeLoggingStart = value; }
        }

        /// <summary>
        /// Gets number of logging iterations
        /// </summary>
        public static int NumberOfIterations
        {
            get { return s_numberOfIterations; }
            set { s_numberOfIterations = value; }
        }

        /// <summary>
        /// Gets duration of each logging iteration in seconds
        /// </summary>
        public static int DurationOfEachIteration
        {
            get { return s_durationOfEachIteration; }
            set { s_durationOfEachIteration = value; }
        }

        /// <summary>
        /// Gets time interval in seconds between two consecutive logging iterations
        /// </summary>
        public static int IntervalBetweenIterations
        {
            get { return s_intervalBetweenIterations; }
            set { s_intervalBetweenIterations = value; }
        }

        /// <summary>
        /// Gets if logging is enabled or not
        /// </summary>
        public static bool LoggingEnabled
        {
            get { return s_loggingEnabled; }
            set { s_loggingEnabled = value; }

        }

        /// <summary>
        /// Gets time interval in seconds after which logger thread should write logs to file
        /// </summary>
        public static int LoggerThreadLoggingInterval
        {
            get { return s_loggerThreadLoggingInterval; }
            set { s_loggerThreadLoggingInterval = value; }
        }

        public bool LoggingExpired
        {
            get { return _loggingExpired; }
        }

        /// <summary>
        /// Loads configurations from application configuration (app.config/web.config)
        /// </summary>
        private static void loadConfiguration()
        {
            try
            {

                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.EnableAPILogging"]))
                    s_loggingEnabled = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["CacheClient.EnableAPILogging"]);
            }
            catch (Exception)
            { }
            try
            {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.TimeBeforeLoggingStart"]))
                {
                    string time = System.Configuration.ConfigurationManager.AppSettings["CacheClient.TimeBeforeLoggingStart"];
                    string[] splitted = time.Split(':');
                    if (splitted.Length == 3)
                        s_timeBeforeLoggingStart = int.Parse(splitted[0]) * 3600 + int.Parse(splitted[1]) * 60 + int.Parse(splitted[2]);
                }
            }
            catch (Exception)
            { }

            try
            {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogIterations"]))
                    s_numberOfIterations = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogIterations"]);
            }
            catch (Exception)
            { }

            try
            {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogIterationLength"]))
                    s_durationOfEachIteration = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogIterationLength"]);
            }
            catch (Exception)
            { }

            try
            {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogDelayBetweenIteration"]))
                    s_intervalBetweenIterations = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CacheClient.APILogDelayBetweenIteration"]);
            }
            catch (Exception)
            { }

            try
            {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["CacheClient.LoggerThreadLoggingInterval"]))
                    s_loggerThreadLoggingInterval = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CacheClient.LoggerThreadLoggingInterval"]);
            }
            catch (Exception)
            { }
        }


        /// <summary>
        /// Checks whether current time instant is in logging interval
        /// </summary>
        /// <returns>true if current time instant is in logging interval, else false</returns>
        public bool IsInLoggingInterval()
        {
            if ((!s_loggingEnabled) || _loggingExpired)
                return false;
            TimeSpan normalizedCurrentInstance = DateTime.Now.Subtract(_loggingStartTime);
            double normalizedSeconds = normalizedCurrentInstance.TotalSeconds;
            if (normalizedSeconds < 0)
                return false;

            int completeIntervalLength = s_durationOfEachIteration + s_intervalBetweenIterations;
            if (normalizedSeconds / (s_numberOfIterations * completeIntervalLength) >= 1)
            {
                _loggingExpired = true;
                return false;
            }

            double fraction = normalizedSeconds - Convert.ToInt32(Math.Floor(normalizedSeconds));
            int normalizedToInterval = Convert.ToInt32(Math.Floor(normalizedSeconds)) % completeIntervalLength;
            double timePassedInCurrentIteration = (double)normalizedToInterval + fraction;
            if (timePassedInCurrentIteration >= s_durationOfEachIteration)
                return false;
            return true;
        }

        /// <summary>
        /// Gets logging iteration number for specified time
        /// </summary>
        /// <param name="loggingTime">Time instant for which logging iteration number is needed</param>
        /// <returns>Logging iteration number</returns>
        public int GetIterationNumber(DateTime loggingTime)
        {
            TimeSpan normalizedCurrentInstance = _loggingStartTime.Subtract(loggingTime);
            double normalizedSeconds = normalizedCurrentInstance.TotalSeconds;
            double loggingInterval = s_durationOfEachIteration + s_intervalBetweenIterations;
            return (int)Math.Round(normalizedSeconds / loggingInterval);
        }
    }
}
