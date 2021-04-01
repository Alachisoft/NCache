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
using Alachisoft.NCache.Common.Stats;


/// <summary>
/// Debug tool, used to output information and errors to a file/screen
/// </summary>
/// 
public class NewTrace
{
    public bool             isInfoEnabled   = false;
    public bool             isErrorEnabled  = false;
    public bool             isWarnEnabled   = false;
    public bool             isDebugEnabled  = false;
    public bool             isFatalEnabled  = false;
    public bool             isCriticalInfoEnabled = false;
    private bool            useHPTime       = false;
    private string          CacheName       = "";
    private string          filePath        = "";

    private StreamWriter    sw              = null;

    #region //------------- Public Properties -------------//
    /// <summary>
    /// True if information traces are enabled.
    /// </summary>
    [CLSCompliant(false)]
    public bool IsInfoEnabled
    {
        get
        {
            return isInfoEnabled;
        }
        set
        {
            isInfoEnabled = value;
        }
    }

    /// <summary>
    /// True if error traces are enabled.
    /// </summary>
	[CLSCompliant(false)]
    public bool IsErrorEnabled
    {
        get
        {
            return isErrorEnabled;
        }
        set
        {
            isErrorEnabled = value;
        }
    }

    /// <summary>
    /// True if warning traces are enabled.
    /// </summary>
	[CLSCompliant(false)]
    public bool IsWarnEnabled
    {
        get
        {
            return isWarnEnabled;
        }
        set
        {
            isWarnEnabled = value;
        }
    }

    /// <summary>
    /// True if debug traces are enabled.
    /// </summary>
	[CLSCompliant(false)]
    public bool IsDebugEnabled
    {
        get
        {
            return isDebugEnabled;
        }
        set
        {
            isDebugEnabled = value;
        }
    }

    /// <summary>
    /// True if fatal error traces are enabled.
    /// </summary>
	[CLSCompliant(false)]
    public bool IsFatalEnabled
    {
        get
        {
            return isFatalEnabled;
        }
        set
        {
            isFatalEnabled = value;
        }
    }

    /// <summary>
    /// True if critical info traces are enabled.
    /// </summary>
    [CLSCompliant(false)]
    public bool IsCriticalInfoEnabled
    {
        get
        {
            return isCriticalInfoEnabled;
        }
        set
        {
            isCriticalInfoEnabled = value;
        }
    }

    /// <summary>
    /// True if fatal error traces are enabled.
    /// </summary>
	[CLSCompliant(false)]
    public bool UseHPTime
    {
        get
        {
            return useHPTime;
        }
        set
        {
            useHPTime = value;
        }
    }
    #endregion 

    public NewTrace()
    {
        isErrorEnabled = true;
    }

    private bool UpdateFileState()
    {
       
        return false;
    }

    /// <summary>
    /// Creates a new output file with the specified prefix.
    /// </summary>
    /// <param name="CacheName">CacheName used as the fielname prefix. Time string is appended to the end of this prefix.</param>
    public void SetOutputStream(string cacheName, string path)
    {
        lock (this)
        {
            if (cacheName != null && sw == null)
            {
                CacheName = cacheName;
                filePath = path;

                string filename = CacheName.ToLower() + "." +
                    Environment.MachineName.ToLower() + "." +
                    DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".log.txt";

                filename = System.IO.Path.Combine(filePath, filename);
                sw = new StreamWriter(filename, false);
                sw.AutoFlush = true;
                string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
                string version = "";
                if (assemblyName != null)
                {
                    string[] parseArray = assemblyName.Split(new char[] { ',' });
                    if (parseArray != null)
                    {
                        for (int i = 0; i < parseArray.Length; i++)
                        {
                            string temp = parseArray.GetValue(i) as string;

							if (temp.Contains("Version"))
                            {
                                version = temp;
                                break;
                            }
                        }
                    }
                }
                criticalInfo("", version);
            }
        }
    }

    public void CloseOutputStream()
    {
        lock (this)
        {
            this.isDebugEnabled = 
                this.isErrorEnabled = 
                this.isFatalEnabled = 
                this.isInfoEnabled = 
                this.isWarnEnabled = false;
            if (sw != null)
            {
                try
                {
                    sw.Close();
                }
                catch (Exception) 
                { 
                }

                sw = null;
            }
        }
    }

    /// <summary>
    /// Information Trace
    /// </summary>
    /// <param name="module">Module responsible for the information</param>
    /// <param name="message">The message to be displayed</param>
    ///[Conditional("DEBUG")] 
    public void info(String module, String message)
    {
        if (isInfoEnabled)
            writeToDebug("[DBG]", module, message);
    }

    public void criticalInfo(String module, String message)
    {
        if (IsCriticalInfoEnabled)
            writeToDebug("[Info]", module, message);
    }
    
    public void info(String message)
    {
        info("", message);
    }

    /// <summary>
    /// Warning Trace
    /// </summary>
    /// <param name="module">Module responsible for the warning</param>
    /// <param name="message">The message to be displayed</param>
    ///[Conditional("DEBUG")] 
    public void warn(String module, String message)
    {
        if (isWarnEnabled)
            writeToDebug("[WARN]", module, message);
    }

    ///[Conditional("DEBUG")] 
    public void warn(String message)
    {
        warn("", message);
    }

    /// <summary>
    /// Error Trace
    /// </summary>
    /// <param name="module">Module responsible for the error</param>
    /// <param name="message">The message to be displayed</param>
    /// [Conditional("DEBUG")] 
    public void error(String module, String message)
    {
        if (isErrorEnabled)
            writeToDebug("[ERR]", module, message);
    }

    public void error(String message)
    {
        error("", message);
    }

    public void fatal(String module, String message)
    {
        if (isFatalEnabled)
            writeToDebug("[FATAL]", module, message);
    }

    public void fatal(String message)
    {
        fatal("", message);
    }

    public void debug(String module, String message)
    {
        if (isDebugEnabled)
            writeToDebug("[DBG]", module, message);
    }

    public void debug(String message)
    {
        debug("", message);
    }

    /// <summary>
    /// Writes the trace to the Debug
    /// </summary>
    /// <param name="type">Type of trace</param>
    /// <param name="module">Module responsible for the error</param>
    /// <param name="message">The message to be displayed</param>
    /// [Conditional("DEBUG")] 
    private void writeToDebug(String type, String module, String message)
    {
        int space1 = 8;
        int space2 = 40;

        if (module.Length == 0)
            space2 = 4;
        
        string line = null;
        if (useHPTime)
            line = HPTime.Now.ToString() + ":  " + type.PadRight(space1 + 4, ' ') + module.PadRight(space2, ' ') + message;
        else
            line = System.DateTime.Now.ToString("dd-MM-yy HH:mm:ss:ffff") + ":  " + type.PadRight(space1, ' ') + module.PadRight(space2, ' ') + message;

        try
        {
            lock (this)
            {
                if (sw != null)
                {
                    sw.WriteLine(line);
                }
            }
        }
        catch (Exception e)
        {
        }
    }
}