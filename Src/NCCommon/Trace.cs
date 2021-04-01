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
using System.IO;
using System.Text;

using System.Diagnostics;
using SysTrace = System.Diagnostics.Trace;
using Alachisoft.NCache.Common.Stats;

/// <summary>
/// Debug tool, used to output information and errors to a file/screen
/// <p><b>Author:</b> Chris Koiak</p>
/// <p><b>Date:</b>  12/03/2003</p>
/// </summary>
/// 
public class Trace
{
	public static bool isInfoEnabled = false;
    public static bool isErrorEnabled = false;
    public static bool isWarnEnabled = false;
    public static bool isDebugEnabled = false;
    public static bool isFatalEnabled = false;
    public static bool useHPTime = false;

	public static void SetOutputStream(string filename)
	{
		if(filename != null)
		{
			TextWriter tw = TextWriter.Synchronized(new StreamWriter(filename, false));
			SysTrace.Listeners.Add(
				new TextWriterTraceListener(tw, "nc")); 
			SysTrace.AutoFlush = true;
		}
		else
		{
			TraceListener listener = SysTrace.Listeners["nc"];
			if(listener != null)
			{
				SysTrace.Listeners.Remove(listener);
				listener.Flush();
				listener.Close();
			}
		}
	}

	/// <summary>
	/// Information Trace
	/// </summary>
	/// <param name="module">Module responsible for the information</param>
	/// <param name="message">The message to be displayed</param>
	///[Conditional("DEBUG")] 
	public static void info(String module, String message) 
	{
		if(isInfoEnabled)
            writeToDebug("[DBG]", module, message);
	}

    
	///[Conditional("DEBUG")] 
	public static void info(String message) 
	{
		info("", message);
	}

	/// <summary>
	/// Warning Trace
	/// </summary>
	/// <param name="module">Module responsible for the warning</param>
	/// <param name="message">The message to be displayed</param>
	public static void warn(String module, String message) 
	{
		if(isWarnEnabled)
			writeToDebug("[WARN]",module,message);
	} 
	
	public static void warn(String message) 
	{
		warn("",message);
	}

	/// <summary>
	/// Error Trace
	/// </summary>
	/// <param name="module">Module responsible for the error</param>
	/// <param name="message">The message to be displayed</param>
	public static void error(String module, String message) 
	{
		if(isErrorEnabled)
			writeToDebug("[ERR]",module,message);
	}

	public static void error(String message) 
	{
		error("",message);
	}

	public static void fatal(String module, String message) 
	{
		if(isFatalEnabled)
			writeToDebug("[FATAL]",module,message);
	}

	public static void fatal(String message) 
	{
		fatal("",message);
	}

	public static void debug(String module, String message) 
	{
		if(isDebugEnabled)
			writeToDebug("[DBG]",module,message);
	}

	public static void debug(String message) 
	{
		debug("",message);
	}

	/// <summary>
	/// Writes the trace to the Debug
	/// </summary>
	/// <param name="type">Type of trace</param>
	/// <param name="module">Module responsible for the error</param>
	/// <param name="message">The message to be displayed</param>
	/// [Conditional("DEBUG")] 
	private static void writeToDebug(String type, String module, String message)
	{
		int space1 = 8;
		int space2 = 40;
        string line = null;
        if(useHPTime)
            line = HPTime.Now.ToString() + ":  " + type.PadRight(space1 + 4, ' ') + module.PadRight(space2, ' ') + message;
        else
            line = System.DateTime.Now.ToString("HH:mm:ss:ffff") + ":  " + type.PadRight(space1, ' ') + module.PadRight(space2, ' ') + message;
	}
}
