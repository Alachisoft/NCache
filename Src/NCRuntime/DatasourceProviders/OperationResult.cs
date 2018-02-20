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
// limitations under the License

using System;

namespace Alachisoft.NCache.Runtime.DatasourceProviders
{
    public class OperationResult
    {
        public enum Status
        {
            Success,
            Failure,
            FailureRetry,
            FailureDontRemove
        }    
        private WriteOperation _writeOperation;
        private bool _updateInCache = false;
        private OperationResult.Status _status = new OperationResult.Status();
        private string _errorMessage;
        private Exception _exception;

        public OperationResult(WriteOperation writeOperation, Status operationStatus)
        {
            this._writeOperation = writeOperation;
            this._status = operationStatus;
        }
        public OperationResult(WriteOperation writeOperation, Status operationStatus, string errorMessage)
        {
            this._writeOperation = writeOperation;
            this._status = operationStatus;
            this._errorMessage = errorMessage;
        }
        public OperationResult(WriteOperation writeOperation, Status operationStatus, Exception exception)
        {
            this._writeOperation = writeOperation;
            this._status = operationStatus;
            this._exception = exception;
        }
        /// <summary>
        /// Specify if item will be updated in cache store after write operation.
        /// </summary>
        public bool UpdateInCache
        {
            get { return _updateInCache; }
            set { _updateInCache = value; }
        }
        /// <summary>
        /// Status of write operation.
        /// </summary>
        public Status DSOperationStatus 
        {
            get { return _status; }
            set { _status = value; }
        }
        /// <summary>
        /// Write operation.
        /// </summary>
        public WriteOperation Operation 
        {
            get { return _writeOperation; }
            set { _writeOperation = value; }
        }
        /// <summary>
        /// Exception associated with write operation.
        /// </summary>
        public Exception Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }
        /// <summary>
        /// Error message associated with write operation.
        /// </summary>
        public string Error
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }
    }
}