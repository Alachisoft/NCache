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
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Diagnostics;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Common.Stats
{
    /// <summary>
    /// HPTime represents the time based on the ticks of High Performance coutners.
    /// It is a relative time not synchronized with system time. The time accuracy
    /// is upto micro seconds.
    /// </summary>
    /// <author></author>
    public class HPTime : IComparable, ICompactSerializable
    {
        private int _hr;
        private int _min;
        private int _sec;
        private int _mlSec;
        private int _micSec;

        private static long _frequency;
        private static long _baseTicks;
        private static object _synObj = new Object();
        private static string _col = ":";

        private double _baseTime;
        private double _baseRem;        

        static HPTime()
        {
            lock (_synObj)
            {
                _frequency = Stopwatch.Frequency;
                _baseTicks = Stopwatch.GetTimestamp();
            }
        }

        /// <summary>
        /// Gets the hours component of the time of this instance.
        /// </summary>
        public int Hours
        {
            get { return _hr; }
        }

        /// <summary>
        /// Gets the hours component of the time of this instance.
        /// </summary>
        public int Minutes
        {
            get { return _min; }
        }

        /// <summary>
        /// Gets the Secnds component of the time of this instance.
        /// </summary>
        public int Seconds
        {
            get { return _sec; }
        }

        /// <summary>
        /// Gets the MilliSecond component of the time of this instance.
        /// </summary>
        public int MilliSeconds
        {
            get { return _mlSec; }
        }

        /// <summary>
        /// Gets the MicroSeconds component of the time of this instance.
        /// </summary>
        public int MicroSeconds
        {
            get { return _micSec; }
        }

        public double BaseTime
        {
            get { return _baseTime; }
            set { _baseRem = value; }
        }

        public double ServerTicks
        {
            get { return _baseRem; }
        }

        /// <summary>
        /// Gets current HP time
        /// </summary>
        public HPTime CurrentTime
        {
            get
            {
                double rem = 0;
                long currentTicks = 0;
                long diff;

                HPTime time = new HPTime();
                currentTicks = Stopwatch.GetTimestamp();

                diff = currentTicks - _baseTicks;
                rem = ((double)diff / (double)_frequency) * 1000;

                //double baseTime = 0;//it will be server time;                
                _baseTime = rem;
                time._baseTime = rem;
                rem += _baseRem;


                time._hr = (int)(rem / 3600000);
                rem = rem - (time._hr * 3600000);

                time._min = (int)rem / 60000;
                rem = rem - (time._min * 60000);

                time._sec = (int)rem / 1000;
                rem = rem - (time._sec * 1000);

                time._mlSec = (int)rem;
                rem = (rem - (double)time._mlSec) * 1000;
                time._micSec = (int)rem;

                return time;
            }
        }

        /// <summary>
        /// Gets current HP time
        /// </summary>
        public static HPTime Now
        {
            get
            {
                double rem = 0;
                long currentTicks = 0;
                long diff;

                HPTime time = new HPTime();
                currentTicks = Stopwatch.GetTimestamp();
                

                diff = currentTicks - _baseTicks;
                rem = ((double)diff / (double)_frequency) * 1000;

                time._hr = (int)(rem / 3600000);
                rem = rem - (time._hr * 3600000);

                time._min = (int)rem / 60000;
                rem = rem - (time._min * 60000);

                time._sec = (int)rem / 1000;
                rem = rem - (time._sec * 1000);

                time._mlSec = (int)rem;
                rem = (rem - (double)time._mlSec) * 1000;
                time._micSec = (int)rem;

                return time;
            }
        }
        /// <summary>
        /// Gets the string representation of the current instance of HP time.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _hr % 24 + _col + _min % 60 + _col + _sec % 60 + _col + (long)_mlSec + _col + _micSec;
        }

        /// <summary>
        /// Gets the string representation of the current instance of HP time.
        /// </summary>
        /// <returns></returns>
        public string ToAbsoluteTimeString()
        {
            return _hr + _col + _min + _col + _sec + _col + (long)_mlSec + _col + _micSec;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is HPTime)
            {
                HPTime other = (HPTime)obj;
                int result = this.Hours.CompareTo(other.Hours);
                if (result == 0)
                {
                    result = this.Minutes.CompareTo(other.Minutes);
                    if (result == 0)
                    {
                        result = this.Seconds.CompareTo(other.Seconds);
                        if (result == 0)
                        {
                            result = this.MilliSeconds.CompareTo(other.MilliSeconds);
                            if (result == 0)
                            {
                                return this.MicroSeconds.CompareTo(other.MicroSeconds);
                            }
                            return result;
                        }
                        return result;
                    }
                    return result;
                }
                return result;
            }
            return 1;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _hr = reader.ReadInt32();
            _micSec = reader.ReadInt32();
            _min = reader.ReadInt32();
            _mlSec = reader.ReadInt32();
            _sec = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_hr);
            writer.Write(_micSec);
            writer.Write(_min);
            writer.Write(_mlSec);
            writer.Write(_sec);
        }

        #endregion

        #region - [Deep Cloning] -

        public HPTime DeepClone(PoolManager poolManager)
        {
            var clonedHPTime = new HPTime();
            clonedHPTime._baseRem = _baseRem;
            clonedHPTime._baseTime = _baseTime;
            clonedHPTime._hr = _hr;
            clonedHPTime._micSec = _micSec;
            clonedHPTime._min = _min;
            clonedHPTime._mlSec = _mlSec;
            clonedHPTime._sec = _sec;

            return clonedHPTime;
        }

        #endregion
    }
}
