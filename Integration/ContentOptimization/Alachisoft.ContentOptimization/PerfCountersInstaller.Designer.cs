//  Copyright (c) 2019 Alachisoft
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
using System.Diagnostics;

namespace Alachisoft.ContentOptimization
{
    partial class PerfCountersInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            const string PRODUCT = "Alachisoft";
            components = new System.ComponentModel.Container();                       

            ContentOptimizationCountersInstaller = new System.Diagnostics.PerformanceCounterInstaller();
            ContentOptimizationCountersInstaller.CategoryName = PRODUCT + ":" + "ContentOptimization";
            ContentOptimizationCountersInstaller.Counters.AddRange(new System.Diagnostics.CounterCreationData[]{
                new CounterCreationData("Viewstate size", "Size of viewstate in bytes.", PerformanceCounterType.AverageCount64),
                new CounterCreationData("Viewstate size base", "Base counter for viewstate size.", PerformanceCounterType.AverageBase),
                new CounterCreationData("Viewstate hits/sec", "Number of times viewstate is successfully served from cache.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData("Viewstate misses/sec", "Number of times viewstate is not successfully served from cache.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData("Viewstate additions/sec", "Number of viewstate add operations per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData("Viewstate requests/sec", "Number of viewstate read requests per second.", PerformanceCounterType.RateOfCountsPerSecond32),
            });            

            this.Installers.AddRange(new System.Configuration.Install.Installer[] {ContentOptimizationCountersInstaller});
        }

        #endregion
    }
}
