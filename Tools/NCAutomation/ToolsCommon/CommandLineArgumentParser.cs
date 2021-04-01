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
using System.Reflection;

using Alachisoft.NCache.Common.Configuration;

using System.Management.Automation;
using Alachisoft.NCache.Common.Enum;


namespace Alachisoft.NCache.Tools.Common
{
    public class CommandLineArgumentParser
    {
        public static void CommandLineParser(ref object obj, string[] args)
        {
            Type type;
            PropertyInfo[] objProps;
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            type = obj.GetType();
            objProps = type.GetProperties();
            ArgumentAttribute orphanAttribute = null;
            PropertyInfo orphanPropInfo = null;


            if (objProps != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    PropertyInfo propInfo;
                    Object[] customParams;
                    int propLoc = i;
                    bool isAssigned = false;
                    for (int j = 0; j < objProps.Length; j++)
                    {
                        propInfo = objProps[j];
                        customParams = propInfo.GetCustomAttributes(typeof(ArgumentAttribute), false);

                        if (customParams != null && customParams.Length > 0)
                        {
                            ArgumentAttribute param = customParams[0] as ArgumentAttribute;
                            try
                            {
                                if (param != null && (param.ShortNotation==args[i] || param.FullName.ToLower() == args[i].ToLower() || param.ShortNotation2 == args[i] || param.FullName2.ToLower() == args[i].ToLower()))
                                {
                                    if (propInfo.PropertyType.FullName == "System.Boolean" || propInfo.PropertyType.FullName == "System.Management.Automation.SwitchParameter")
                                    {
                                        bool value = false;
                                        if (propInfo.PropertyType.FullName == "System.Management.Automation.SwitchParameter")
                                        {
                                            object objvalue = param.DefaultValue;
                                            value = Convert.ToBoolean(objvalue);
                                            objvalue = !value;
                                            SwitchParameter paramSwitch = new SwitchParameter(!value);
                                            propInfo.SetValue(obj, paramSwitch, null);
                                            isAssigned = true;
                                            break;
                                        }
                                        else
                                        {
                                             value = (bool)param.DefaultValue;

                                            if (value)
                                                propInfo.SetValue(obj, false, null);
                                            else
                                                propInfo.SetValue(obj, true, null);
                                            isAssigned = true;
                                        }
                                        
                                        break;
                                    }
                                    else
                                    {
                                        int index = i + 1;
                                        if (index <= (args.Length - 1))
                                        {
                                            
                                            object value;
                                             if (propInfo.PropertyType.FullName .Contains((new CacheTopologyParam()).GetType().ToString() ))
                                             {
                                                 value = GetTopologyType(args[++i]);
                                             }
                                             else if (propInfo.PropertyType.FullName.Contains ((new ReplicationStrategyParam()).GetType().ToString()))
                                             {
                                                 value = GetReplicatedStrategy(args[++i]);
                                             }
                                             else if (propInfo.PropertyType.FullName.Contains( (new EvictionPolicyParam()).GetType().ToString()))
                                             {
                                                 value = GetEvictinPolicy(args[++i]);
                                             }
                                           
                                            else if (propInfo.PropertyType.FullName.Contains((new SerializationFormatParam()).GetType().ToString()))
                                            {
                                                value = GetSerializationFormatType(args[++i]);
                                            }
                                            else
                                             {
                                                 value = configBuilder.ConvertToPrimitive(propInfo.PropertyType, args[++i], null);
                                             }
                                            if (propInfo.PropertyType.IsAssignableFrom(value.GetType()))
                                            {
                                                propInfo.SetValue(obj, value, null);
                                                isAssigned = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (param != null && ((string.IsNullOrEmpty(param.ShortNotation) && string.IsNullOrEmpty(param.FullName)) || (string.IsNullOrEmpty(param.ShortNotation2) && string.IsNullOrEmpty(param.FullName2))))
                                {
                                    if (orphanAttribute == null && !isAssigned)
                                    {
                                        orphanAttribute = param;
                                        orphanPropInfo = propInfo;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Can not set the value for attribute " + param.ShortNotation + " Error :" + e.Message.ToString());
                            }

                        }
                    }
                    if (!isAssigned)
                    {
                        if (orphanAttribute != null && orphanPropInfo != null)
                        {
                            if (orphanPropInfo.GetValue(obj, null) != null)
                            {
                                object value = configBuilder.ConvertToPrimitive(orphanPropInfo.PropertyType, args[i], null);
                                if (orphanPropInfo.PropertyType.IsAssignableFrom(value.GetType()))
                                {
                                    orphanPropInfo.SetValue(obj, value, null);
                                }
                            }
                        }
                    }
                }
            }
        }
        private static object GetReplicatedStrategy(string value)
        {
            if (value.ToLower().Equals("sync"))
            {
                return ReplicationStrategyParam.Sync;
            }
            else if (value.ToLower().Equals("async"))
            {
                return ReplicationStrategyParam.Async;
            }
            else
            {
                return null;
            }


        }
  
        private static object GetEvictinPolicy(string evictionPolicy)
        {
            object evictionType =null;

            switch (evictionPolicy.ToLower())
            {
                case "priority":
                    evictionType = EvictionPolicyParam.Priority;
                    break;

                case "none":
                    evictionType = EvictionPolicyParam.None;
                    break;


                default:
                    evictionType = null;
                    break;
            }
            return evictionType;
        }
        private static object GetTopologyType(string topologyName)
        {

            CacheTopologyParam?   topology = new CacheTopologyParam();
            switch (topologyName.ToLower())
            {
                case "local":
                    topology = CacheTopologyParam.Local;
                    return topology;

                case "replicated":
                    topology = CacheTopologyParam.Replicated;
                    return topology;

                case "partitioned":
                    topology = CacheTopologyParam.Partitioned;
                    return topology;
                default:
                    return null;

            }
        }

        private static object GetSerializationFormatType(string serializationFormat)
        {
            SerializationFormatParam? format = new SerializationFormatParam();

            switch(serializationFormat.ToLower())
            {
                case "binary":
                    format = SerializationFormatParam.Binary;
                    return format;

                case "json":
                    format = SerializationFormatParam.Json;
                    return format;

                default:
                    format = SerializationFormatParam.Binary;
                    return format;
            }
        }

    }
}
