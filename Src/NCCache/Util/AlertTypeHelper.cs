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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.Propagator;

namespace Alachisoft.NCache.Caching.Util
{

	//EmailNotifier: AlertTypeHelper
	public class AlertTypeHelper
	{
		public static AlertNotificationTypes Initialize(IDictionary properties)
		{
			AlertNotificationTypes alertTypes = new AlertNotificationTypes();

			if (properties.Contains("cache-stop"))
			{
				alertTypes.CacheStop = Convert.ToBoolean(properties["cache-stop"]);
			}

			if (properties.Contains("cache-start"))
			{
				alertTypes.CacheStart = Convert.ToBoolean(properties["cache-start"]);
			}

			if (properties.Contains("node-left"))
			{
				alertTypes.NodeLeft = Convert.ToBoolean(properties["node-left"]);
			}

			if (properties.Contains("node-joined"))
			{
				alertTypes.NodeJoined = Convert.ToBoolean(properties["node-joined"]);
			}
			
			if (properties.Contains("state-transfer-started"))
			{
				alertTypes.StartTransferStarted = Convert.ToBoolean(properties["state-transfer-started"]);
			}

			if (properties.Contains("state-transfer-stop"))
			{
				alertTypes.StartTransferStop = Convert.ToBoolean(properties["state-transfer-stop"]);
			}

			if (properties.Contains("state-transfer-error"))
			{
				alertTypes.StartTransferError = Convert.ToBoolean(properties["state-transfer-error"]);
			}

			if (properties.Contains("service-start-error"))
			{
				alertTypes.ServiceStartError = Convert.ToBoolean(properties["service-start-error"]);
			}

			if (properties.Contains("cache-size"))
			{
				alertTypes.CacheSize = Convert.ToBoolean(properties["cache-size"]);
			}

			if (properties.Contains("general-error"))
			{
				alertTypes.GeneralError = Convert.ToBoolean(properties["general-error"]);
			}

			if (properties.Contains("licensing-error"))
			{
				alertTypes.LicensingError = Convert.ToBoolean(properties["licensing-error"]);
			}

			if (properties.Contains("configuration-error"))
			{
				alertTypes.ConfigurationError = Convert.ToBoolean(properties["configuration-error"]);
			}

			if (properties.Contains("security-error"))
			{
				alertTypes.SecurityError = Convert.ToBoolean(properties["security-error"]);
			}

			if (properties.Contains("general-info"))
			{
				alertTypes.GeneralInfo = Convert.ToBoolean(properties["general-info"]);
			}

			if (properties.Contains("unhandled-exceptions"))
			{
				alertTypes.UnHandledException = Convert.ToBoolean(properties["unhandled-exceptions"]);
			}

            if (properties.Contains("partial-connectivity-detected"))
            {
                alertTypes.PartialConnectivity = Convert.ToBoolean(properties["partial-connectivity-detected"]);
            }

			return alertTypes;
		}
	}
}
