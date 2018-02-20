// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Analysis.Generator;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Analysis.Renderer;
using System.Diagnostics;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;

namespace Alachisoft.NCache.Integrations.EntityFramework.Analysis
{
    /// <summary>
    /// Manage analysis stats and log them
    /// </summary>
    internal sealed class AnalysisManager : IDisposable
    {
        /// <summary>
        /// Single istance of <c>AnalysisManager</c>
        /// </summary>
        public static AnalysisManager Instance;

        private AnalysisPolicyElement effectivePolicy;
        private CustomPolicyGenerator customPolicyGen;
        private Timer timer;

        /// <summary>
        /// Static constructor
        /// </summary>
        static AnalysisManager()
        {
            Instance = new AnalysisManager();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private AnalysisManager()
        {
            this.timer = null;            
            //ConfigurationBuilder.RegisterRootConfigurationObject(typeof(CustomPolicyElement));
            EFCachingConfiguration.Instance.ConfigurationUpdated += new EventHandler<ConfiguraitonUpdatedEventArgs>(Instance_ConfigurationUpdated);
        }

        /// <summary>
        /// Called when configuraion changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_ConfigurationUpdated(object sender, ConfiguraitonUpdatedEventArgs e)
        {
            if (e != null && e.Configuration != null)
            {
                ///Analysis was running and user changed the mode. We need to stop analysis and write report
                if (this.IsRunning && e.Configuration.Mode != ApplicationConfigurationElement.AppMode.Analysis)
                {
                    this.Stop();
                }
                ///Start the analysis
                else if (!this.IsRunning && e.Configuration.Mode == ApplicationConfigurationElement.AppMode.Analysis)
                {
                    if (e.Configuration.AnalysisPolicy != null)
                    {
                        this.effectivePolicy = e.Configuration.AnalysisPolicy.Clone() as AnalysisPolicyElement;
                    }
                    else
                    {
                        this.effectivePolicy = null;
                    }
                    this.Start(this.effectivePolicy);
                }
            }
        }

        /// <summary>
        /// Get a value determining whether <c>AnalysisManager</c> is running or not
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Start analysis
        /// </summary>
        /// <param name="policy">Analysis policy</param>
        public void Start(AnalysisPolicyElement policy)
        {
            lock (this)
            {
                if (policy == null)
                {
                    Logger.Instance.TraceError("No 'analysis-policy' found in configuration. Analysis cannot start");
                    return;
                }
                this.customPolicyGen = new CustomPolicyGenerator(policy);

                if (this.IsRunning)
                {
                    return;
                }

                if (policy.AnalysisTime > 0)
                {
                    this.StartTimer(policy.AnalysisTime);
                }
                this.effectivePolicy = policy.Clone() as AnalysisPolicyElement;
                this.IsRunning = true;
            }
        }

        /// <summary>
        /// Start the timer
        /// </summary>
        /// <param name="analysisTime"></param>
        private void StartTimer(int analysisTime)
        {
            this.timer = new Timer();
            this.timer.AutoReset = false;
            this.timer.Interval = analysisTime * 60000;
            this.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            this.timer.Start();
        }

        /// <summary>
        /// Stop analysis
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                if (!this.IsRunning)
                {
                    return;
                }
                if (this.timer != null)
                {
                    this.timer.Stop();
                    this.timer.Close();
                    this.timer.Dispose();
                    this.timer = null;                    
                }

                this.IsRunning = false;
            }

            this.GenrateReport();
        }

        /// <summary>
        /// Called when the analysiz timer elapse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Stop();
        }        

        /// <summary>
        /// Generates the report from the collected data
        /// </summary>
        private void GenrateReport()
        {
            string reportPath = null;
            reportPath = this.effectivePolicy.LogPath;
            if (!reportPath.IsNullOrEmpty())
            {
                if (!Directory.Exists(reportPath))
                {
                    reportPath = null;
                    Logger.Instance.TraceError("'log-path' for creating analysis reports do not exists.\r\n" +
                        string.Format("Reports will be generated at default path: ({0})", this.effectivePolicy.DefaultLogPath));
                }
            }
            ///If no log path is specified, go for default log path
            ///which is = <install-dir>\log-files\efcaching-analysis-logs\
            if (reportPath.IsNullOrEmpty())
            {
                reportPath = this.effectivePolicy.DefaultLogPath;
            }

            string applicationId = string.Empty;
            if (!Application.Instance.ApplicationId.IsNullOrEmpty())
            {
                applicationId = Application.Instance.ApplicationId + ".";
            }

            reportPath = Path.Combine(reportPath,
                string.Format("{0}{1}.{2}.analysis.txt", applicationId, Process.GetCurrentProcess().Id.ToString(), DateTime.Now.ToString("dd-MM-yy HH-mm-ss")));
            
            try
            {
                using (IRenderer<CustomPolicyElement> renderer = new FileRenderer(reportPath))
                {
                    CustomPolicyElement customPolicy = this.customPolicyGen.Generate();
                    if (customPolicy != null)
                    {
                        renderer.Flush(customPolicy);
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Instance.TraceError(exc.Message);
            }
        }

        /// <summary>
        /// Analyze a query
        /// </summary>
        /// <param name="query">Query command</param>
        public void AnalyzeQuery(Query query)
        {
            if (this.IsRunning)
            {   
                this.customPolicyGen.AnalyzeQuery(query);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.Stop();
        }

        #endregion
    }
}
