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
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Defines extension methods
/// </summary>
internal static class ExtensionMethods
{
    /// <summary>
    /// Removes '\r', '\n', and '\t' characters from the string
    /// </summary>
    /// <param name="src">Source string</param>
    /// <returns>Resulting string without tabs and newline characters</returns>
    public static string StripTabsAndNewlines(this string src)
    {
        char[] newChars = new char[src.Length];
        int newStringIndex = 0;
        bool suppressWhiteSpace = false;
        for (int i = 0; i < src.Length; ++i)
        {
            char c = src[i];
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                    if (!suppressWhiteSpace)
                    {
                        newChars[newStringIndex++] = ' ';
                        suppressWhiteSpace = true;
                    }
                    break;
                case ' ':
                    if(!suppressWhiteSpace)
                    {
                        newChars[newStringIndex++] = c;
                        suppressWhiteSpace = true;
                    }
                    break;
                default:
                    newChars[newStringIndex++] = c;
                    suppressWhiteSpace = false;
                    break;
            }
        }

        return new string(newChars, 0, newStringIndex);
    }

    /// <summary>
    /// Determine whether this string is null or empty
    /// </summary>
    /// <param name="src">Source string</param>
    /// <returns>True is string is null or empty, false otherwise</returns>
    public static bool IsNullOrEmpty(this string src)
    {
        return string.IsNullOrEmpty(src);
    }

    /// <summary>
    /// Deep clone the array, only if items in the array are cloneable
    /// </summary>
    /// <param name="array">Array to deep clone</param>
    /// <returns>Copy of array</returns>
    public static Array DeepClone(this Array array)
    {
        if (array == null)
        {
            return array;
        }
        if (array.Length == 0)
        {
            return (Array)array.Clone();
        }

        object[] clone = new object[array.Length];

        for (int i = 0; i < array.Length; i++)
        {
            object obj = array.GetValue(i);
            if (obj != null)
            {
                if (obj is ICloneable)
                {
                    obj = ((ICloneable)obj).Clone();
                }
                clone.SetValue(obj, i);
            }
        }

        return clone;
    }
}

