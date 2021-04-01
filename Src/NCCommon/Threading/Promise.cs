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
using System.Threading;

namespace Alachisoft.NCache.Common.Threading
{
	/// <remarks>
	/// The caller may choose to check
	/// for the result at a later time, or immediately and it may block or not. Both the caller and responder have to
	/// know the promise.
	/// </remarks>
	/// <summary>
	/// Allows a thread to submit an asynchronous request and to wait for the result.
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public class Promise 
	{
		/// <summary>The result of the request</summary>
		Object _result=null;
		/// <summary>Used to wait on the result</summary>
		Object _mutex=new Object();
    
		/// <summary>
		/// If result was already submitted, returns it immediately, else blocks until
		/// results becomes available. 
		/// </summary>
		/// <param name="timeout">Maximum time to wait for result.</param>
		/// <returns>Promise result</returns>
		public Object WaitResult(long timeout) 
		{
			Object ret=null;

			lock(_mutex) 
			{
				if(_result != null) 
				{
					ret=_result;
					_result=null;
					return ret;
				}
				if(timeout <= 0) 
				{
					try 
					{
						Monitor.Wait(_mutex);
					} 
					catch(Exception ex) 
					{
                        Trace.error("Promise.WaitResult", "exception=" + ex.ToString());
					}
				}
				else 
				{
					try 
					{
						Monitor.Wait(_mutex,(int)timeout,true);} 
					catch(Exception ex) 
					{
                        Trace.error("Promise.WaitResult:", "exception=" + ex.ToString());
					}
				}

				if(_result != null) 
				{
					ret=_result;
					_result=null;
					return ret;
				}
				return null;
			}
		}
    
		/// <summary>
		/// Checks whether result is available. Does not block.
		/// </summary>
		/// <returns>Result if available</returns>
		public Object IsResultAvailable 
		{
            get
            {
                lock (_mutex)
                {
                    return _result;
                }
            }
		}

		/// <summary>
		/// Sets the result and notifies any threads waiting for it
		/// </summary>
		/// <param name="obj">Result of request</param>
		public void SetResult(Object obj) 
		{
			lock(_mutex) 
			{
				_result=obj;
				Monitor.PulseAll(_mutex);
			}
		}


		/// <summary>
		/// Clears the result and causes all waiting threads to return
		/// </summary>
		public void Reset() 
		{
			lock(_mutex) 
			{
				_result=null;
				Monitor.PulseAll(_mutex);
			}
		}

		/// <summary>
		/// String representation of the result
		/// </summary>
		/// <returns>String representation of the result</returns>
		public override String ToString() 
		{
			return "result=" + _result;
		}

	}
}
