using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Tools.Common
{
    public enum CacheTopologyParam
    {
        Local,
        Replicated,
    }
    public enum EvictionPolicyParam
    {
        Priority,
        None
    }
  
    public enum SerializationFormatParam
    {
        Binary,
        Json
    }

    public enum KeyTypeParam
    {
        License,
        Extension
    }

    public enum CacheSettingsParam
    {
        AutoStart,
    }
    public enum RegisterationType
    {
        CacheServer,
        RemoteClient,
        Developer
    }
    public enum ActivationRegisterationType
    {
        CacheServer,
        RemoteClient
    }
    public enum BinaryChoice
    {
        Yes,
        No
    }
    public enum LicenseDuration
    {
        Standard,
        Monthly,
        Hourly
    }
}