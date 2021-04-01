using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Collections;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Tools.Common
{
    public class SeperateHostArgumentParser
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

                        // propInfo = objProps[propLoc];
                        //customAttribs = propInfo.GetCustomAttributes(typeof(ToolAttrib), false);
                        if (customParams != null && customParams.Length > 0)
                        {
                            ArgumentAttribute param = customParams[0] as ArgumentAttribute;
                            try
                            {
                                if (param != null && (param.ShortNotation == args[i] || param.FullName.ToLower() == args[i].ToLower() || param.ShortNotation2 == args[i] || param.FullName2.ToLower() == args[i].ToLower()))
                                {
                                    if (propInfo.PropertyType.FullName == "System.Boolean")
                                    {
                                        bool value = (bool)param.DefaultValue;
                                        if (value)
                                            propInfo.SetValue(obj, false, null);
                                            else
                                            propInfo.SetValue(obj, true, null);
                                        isAssigned = true;
                                        break;
                                    }
                                    else
                                    {
                                        int index = i + 1;
                                        if (index <= (args.Length - 1))
                                        {

                                            object value = configBuilder.ConvertToPrimitive(propInfo.PropertyType, args[++i], null);
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

    }
}
