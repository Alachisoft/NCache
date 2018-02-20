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
// $Id: RequestCorrelator.java,v 1.12 2004/09/05 04:54:21 ovidiuf Exp $
namespace Alachisoft.NGroups.Blocks
{

    /// <summary> The runnable for an incoming request which is submitted to the
    /// dispatcher
    /// </summary>
    internal class RequestCorrelatorRequest : IThreadRunnable
    {
        private RequestCorrelator enclosingInstance;
        public RequestCorrelator Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        public Message req;

        public RequestCorrelatorRequest(RequestCorrelator enclosingInstance, Message req)
        {
            this.enclosingInstance = enclosingInstance;
            this.req = req;
        }

        public virtual void Run()
        {
            Enclosing_Instance.handleRequest(req, null);
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (req != null)
            {
                sb.Append("req=" + req + ", headers=" + Global.CollectionToString(req.Headers));
            }
            return sb.ToString();
        }
    }
}
