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
using System.IO;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Config;

namespace Alachisoft.NCache.Integrations.EntityFramework.Analysis.Renderer
{
    internal class FileRenderer : IRenderer<CustomPolicyElement>
    {
        private StreamWriter writer;
        private string filePath;

        public FileRenderer(string filePath)
        {
            this.filePath = filePath;
            //this.writer = new StreamWriter(filePath);            
        }

        #region IRenderer Members

        /// <summary>
        /// Flush analysis report to renderer
        /// </summary>
        /// <param name="data">Data to render</param>
        public void Flush(CustomPolicyElement data)
        {
            if (writer == null)
            {
                writer = new StreamWriter(filePath);
            }
            if (data == null)
            {
                return;
            }

            ConfigurationBuilder cb = new ConfigurationBuilder(new object[] { data });
            cb.RegisterRootConfigurationObject(typeof(CustomPolicyElement));
            string xml = cb.GetXmlString();

            this.writer.Write(xml);
            this.writer.Flush();                
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (this.writer != null)
            {
                try
                {
                    this.writer.Close();
                }
                catch (Exception) { }
                this.writer.Dispose();
                this.writer = null;
            }
        }

        #endregion
    }
}
