using Alachisoft.NCache.Runtime.Exceptions;
using System;

namespace Alachisoft.NCache.Common.ResponseSerialization
{ 
    internal class ResponseBaseBuilder
    {
        internal Protobuf.Response GetExceptionResponse(Exception exc, long requestId, int commandID)
        {
            Protobuf.Exception ex = new Protobuf.Exception();
            ex.message = exc.Message;
            ex.exception = exc.ToString();
            if (exc is InvalidReaderException)
            {
                InvalidReaderException temp = (InvalidReaderException)exc;
                ex.type = Protobuf.Exception.Type.INVALID_READER_EXCEPTION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is OperationFailedException)
            {
                OperationFailedException temp = (OperationFailedException)exc;
                ex.type = Protobuf.Exception.Type.OPERATIONFAILED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is Runtime.Exceptions.AggregateException)
            {
                Runtime.Exceptions.AggregateException temp = (Runtime.Exceptions.AggregateException)exc;
                ex.type = Protobuf.Exception.Type.AGGREGATE;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is ConfigurationException)
            {
                ConfigurationException temp = (ConfigurationException)exc;
                ex.type = Protobuf.Exception.Type.CONFIGURATION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is SecurityException)
            {
                SecurityException temp = (SecurityException)exc;
                ex.type = Protobuf.Exception.Type.SECURITY;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }

            else if (exc is MaintenanceException)
            {
                MaintenanceException temp = (MaintenanceException)exc;
                ex.type = Protobuf.Exception.Type.MAINTENANCE_EXCEPTION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is VersionException)
            {
                VersionException tempEx = (VersionException)exc;
                ex.type = Protobuf.Exception.Type.CONFIGURATON_EXCEPTION;
                ex.errorCode = tempEx.ErrorCode;
                ex.stackTrace = tempEx.StackTrace;
            }
            else if (exc is OperationNotSupportedException)
            {
                OperationNotSupportedException temp = (OperationNotSupportedException)exc;
                ex.type = Protobuf.Exception.Type.NOTSUPPORTED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamAlreadyLockedException)
            {
                StreamAlreadyLockedException temp = (StreamAlreadyLockedException)exc;
                ex.type = Protobuf.Exception.Type.STREAM_ALREADY_LOCKED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamCloseException)
            {
                StreamCloseException temp = (StreamCloseException)exc;
                ex.type = Protobuf.Exception.Type.STREAM_CLOSED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamInvalidLockException)
            {
                StreamInvalidLockException temp = (StreamInvalidLockException)exc;
                ex.type = Protobuf.Exception.Type.STREAM_INVALID_LOCK;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamNotFoundException)
            {
                StreamNotFoundException temp = (StreamNotFoundException)exc;
                ex.type = Protobuf.Exception.Type.STREAM_NOT_FOUND;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamException)
            {
                StreamException temp = (StreamException)exc;
                ex.type = Protobuf.Exception.Type.STREAM_EXC;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is TypeIndexNotDefined)
            {
                TypeIndexNotDefined temp = (TypeIndexNotDefined)exc;
                ex.type = Protobuf.Exception.Type.TYPE_INDEX_NOT_FOUND;
            }
            else if (exc is AttributeIndexNotDefined)
            {
                ex.type = Protobuf.Exception.Type.ATTRIBUTE_INDEX_NOT_FOUND;
            }
            else if (exc is StateTransferInProgressException)
            {
                ex.type = Protobuf.Exception.Type.STATE_TRANSFER_EXCEPTION;
            }
           
            else if (exc is CacheException)
            {
                CacheException temp = (CacheException)exc;
                ex.type = Protobuf.Exception.Type.GENERALFAILURE;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else
            {
                ex.type = Protobuf.Exception.Type.GENERALFAILURE;
                ex.stackTrace = exc.StackTrace;
            }

            Protobuf.Response response = new Protobuf.Response();
            response.requestId = requestId;

            response.commandID = commandID;
            response.exception = ex;
            response.responseType = Protobuf.Response.Type.EXCEPTION;

            return response;
        }
    }
}
