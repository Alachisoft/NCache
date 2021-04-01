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

namespace Alachisoft.NCache.Common.Propagator
{
	public class AlertNotificationTypes
	{
		private bool _cacheStop;
		private bool _cacheStart;
		private bool _nodeLeft;
		private bool _nodeJoined;
		private bool _stateTransferStop;
		private bool _stateTransferStarted;
		private bool _stateTransferError;
		private bool _serviceStartError;
		private bool _cacheSize;
		private bool _generalError;
		private bool _licensingError;
		private bool _configurationError;
		private bool _securityError;
		private bool _generalInformation;
		private bool _unhandledExceptions;
        private bool _partialConnectivityDetected;

		public bool CacheStop
		{
			get { return _cacheStop; }
			set { _cacheStop = value; }
		}

		public bool CacheStart
		{
			get { return _cacheStart; }
			set { _cacheStart = value; }
		}

		public bool NodeLeft
		{
			get { return _nodeLeft; }
			set { _nodeLeft = value; }
		}

		public bool NodeJoined
		{
			get { return _nodeJoined; }
			set { _nodeJoined = value; }
		}

		public bool StartTransferStarted
		{
			get { return _stateTransferStarted; }
			set { _stateTransferStarted = value; }
		}

		public bool StartTransferStop
		{
			get { return _stateTransferStop; }
			set { _stateTransferStop = value; }
		}

		public bool StartTransferError
		{
			get { return _stateTransferError; }
			set { _stateTransferError = value; }
		}

		public bool ServiceStartError
		{
			get { return _serviceStartError; }
			set { _serviceStartError = value; }
		}

		public bool CacheSize
		{
			get { return _cacheSize; }
			set { _cacheSize = value; }
		}

		public bool GeneralError
		{
			get { return _generalError; }
			set { _generalError = value; }
		}

		public bool LicensingError
		{
			get { return _licensingError; }
			set { _licensingError = value; }
		}

		public bool ConfigurationError
		{
			get { return _configurationError; }
			set { _configurationError = value; }
		}

		public bool SecurityError
		{
			get { return _securityError; }
			set { _securityError = value; }
		}

		public bool GeneralInfo
		{
			get { return _generalInformation; }
			set { _generalInformation = value; }
		}

		public bool UnHandledException
		{
			get { return _unhandledExceptions; }
			set { _unhandledExceptions = value; }
		}

        public bool PartialConnectivity
        {
            get { return _partialConnectivityDetected; }
            set { _partialConnectivityDetected = value; }
        }
	}
}
