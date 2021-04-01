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

namespace Alachisoft.NCache.Common.RPCFramework
{
    public class RPCService<TargetObject>
    {
        RPCMethodCache<TargetObject> _rpcCache;

        public RPCService(IRPCTargetObject<TargetObject> targetObject)
        {
            _rpcCache = new RPCMethodCache<TargetObject>(targetObject);
        }

        public object InvokeMethodOnTarget(string methodName, int overload, object[] arguments)
        {
            ITargetMethod targetMethod = _rpcCache.GetTargetMethod(methodName, overload);

            if (targetMethod == null)
                throw new System.Reflection.TargetInvocationException("Target method not found (Method: " + methodName + " , overload : " + overload +")",null);

            if (targetMethod.GetNumberOfArguments() != arguments.Length)
                throw new System.Reflection.TargetParameterCountException();

            object returnVal = targetMethod.Invoke(arguments);

            return returnVal;
        }

    }
}
