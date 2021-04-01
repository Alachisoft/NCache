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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.Stats
{
    /// <summary>
    /// Class that is useful in capturing statistics. It uses High performnace counters for
    /// the measurement of the time.
    /// </summary>
    public class HPTimeStats :ICompactSerializable
    {
        /// <summary> Total number of samples collected for the statistics. </summary>
        private long _runCount;
        /// <summary> Timestamp for the begining of a sample. </summary>
        private long _lastStart;
        /// <summary> Timestamp for the end of a sample. </summary>
        private long _lastStop;
        /// <summary> Total time spent in sampling, i.e., acrued sample time. </summary>
        private double _totalTime;
        /// <summary> Best time interval mesaured during sampling. </summary>
        private double _bestTime;
        /// <summary> Worst time interval mesaured during sampling. </summary>
        private double _worstTime;
        /// <summary> Avg. time interval mesaured during sampling. </summary>
        private double _avgTime;
        /// <summary> Total number of samples collected for the statistics. </summary>
        private long _totalRunCount;
        private float _avgCummulativeOperations;

        private double _worstThreshHole = double.MaxValue;

        private long _worstOccurance;

        private static double _frequency;

        static HPTimeStats()
        {
            long freq = 0;
            freq = Stopwatch.Frequency;
            _frequency = (double)freq / (double)1000;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public HPTimeStats()
        {

            Reset();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public HPTimeStats(double worstThreshHoleValue)
        {
            Reset();
            _worstThreshHole = worstThreshHoleValue;
        }

        /// <summary>
        /// Returns the total numbre of runs in the statistics capture.
        /// </summary>
        public long Runs
        {
            get { lock (this) { return _runCount; } }
            set { lock (this) _runCount = value; }
        }

        /// <summary>
        /// Gets or sets the threshhold value for worst case occurance count.
        /// </summary>
        public double WorstThreshHoldValue
        {
            get { return _worstThreshHole; }
            set { _worstThreshHole = value; }
        }

        /// <summary>
        /// Gets the number of total worst cases occurred.
        /// </summary>
        public long TotalWorstCases
        {
            get { return _worstOccurance; }
        }

        /// <summary>
        /// Returns the total time iterval spent in sampling
        /// </summary>
        public double Total
        {
            get { lock (this) { return _totalTime; } }
        }

        /// <summary>
        /// Returns the time interval for the last sample
        /// </summary>
        public double Current
        {
            get { lock (this) { return (double)(_lastStop - _lastStart)/(double)_frequency; } }
        }

        /// <summary>
        /// Returns the best time interval mesaured during sampling
        /// </summary>
        public double Best
        {
            get { lock (this) { return _bestTime; } }
        }

        /// <summary>
        /// Returns the avg. time interval mesaured during sampling
        /// </summary>
        public double Avg
        {
            get { lock (this) { return _avgTime; } }
        }

        public float AvgOperations
        {
            get { lock (this) { return _avgCummulativeOperations; } }
        }
        /// <summary>
        /// Returns the worst time interval mesaured during sampling
        /// </summary>
        public double Worst
        {
            get { lock (this) { return _worstTime; } }
        }

        /// <summary>
        /// Resets the statistics collected so far.
        /// </summary>
        public void Reset()
        {
            _runCount = 0;
            _totalTime = _bestTime = _worstTime = _worstOccurance = 0;
            _avgTime = 0;
            _avgCummulativeOperations = 0;
        }

        /// <summary>
        /// Timestamps the start of a sampling interval.
        /// </summary>
        public void BeginSample()
        {
            _lastStart = Stopwatch.GetTimestamp();  // POTeam DateTime.UtcNow.Ticks; 
        }


        /// <summary>
        /// Timestamps the end of interval and calculates the sample time
        /// </summary>
        public void EndSample()
        {
            lock (this)
            {
                _lastStop = Stopwatch.GetTimestamp();   // POTeam DateTime.UtcNow.Ticks; 
                AddSampleTime(Current);
            }
        }

        /// <summary>
        /// Timestamps the end of interval and calculates the sample time
        /// </summary>
        public void EndSample(int runcount)
        {
            lock (this)
            {
                _lastStop = Stopwatch.GetTimestamp();
                AddSampleTime(Current, runcount);
            }
        }
        /// <summary>
        /// Adds a specified sample time to the statistics and updates the run count
        /// </summary>
        /// <param name="time">sample time in milliseconds.</param>
        public void AddSampleTime(double time)
        {
            lock (this)
            {

                _runCount++;
                _totalRunCount++;

                if (_runCount == 1)
                {
                    _avgTime = _totalTime = _bestTime = _worstTime = time;
                }
                else
                {
                    _totalTime += time;
                    if (time < _bestTime) _bestTime = time;
                    if (time > _worstTime) _worstTime = time;
                    if (time > _worstThreshHole) _worstOccurance += 1;
                    _avgTime = (double)_totalTime / (double)_runCount;
                }


                if (_totalTime < 1000)
                    _avgCummulativeOperations = _runCount;
                else
                    _avgCummulativeOperations = (float)_runCount * 1000 / (float)_totalTime;

            }
        }

        /// <summary>
        /// Adds a specified sample time to the statistics and updates the run count
        /// </summary>
        /// <param name="time">sample time in milliseconds.</param>
        public void AddSampleTime(double time, int runcount)
        {
            lock (this)
            {

                _runCount += runcount;
                _totalRunCount += runcount;

                if (_runCount == 1)
                {
                    _avgTime = _totalTime = _bestTime = _worstTime = time;
                }
                else
                {
                    _totalTime += time;
                    if (time < _bestTime) _bestTime = time;
                    if (time > _worstTime) _worstTime = time;
                    if (time > _worstThreshHole) _worstOccurance += 1;
                    _avgTime = (float)_totalTime / (float)_runCount;
                }

                if (_totalTime < 1000)
                    _avgCummulativeOperations = _runCount;
                else
                    _avgCummulativeOperations = (float)_runCount * 1000 / (float)_totalTime;
            }
        }


        /// <summary>
        /// Gets the total run count for the samples
        /// </summary>
        public long TotalRunCount
        {
            get { return _totalRunCount; }
        }

        /// <summary>
        /// Override converts to string equivalent.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            lock (this)
            {
                string retval = "[Runs: " + _runCount + ", ";
                retval += "Best(ms): " + _bestTime + ", ";
                retval += "Avg.(ms): " + _avgTime + ", ";
                retval += "Worst(ms): " + _worstTime + ", ";
                retval += "WorstThreshHole(ms): " + _worstThreshHole + ", ";
                retval += "Worst cases: " + _worstOccurance + "]";

                return retval;
            }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _runCount = reader.ReadInt64();
            _avgTime = reader.ReadDouble();
            _bestTime = reader.ReadDouble();
            _lastStart = reader.ReadInt64();
            _lastStop = reader.ReadInt64();
            _worstThreshHole = reader.ReadDouble();
            _worstTime = reader.ReadDouble();
            _totalRunCount = reader.ReadInt64();
            _totalTime = reader.ReadDouble();
            _worstOccurance = reader.ReadInt64();
            _avgCummulativeOperations = reader.ReadSingle();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_runCount);
            writer.Write(_avgTime);
            writer.Write(_bestTime);
            writer.Write(_lastStart);
            writer.Write(_lastStop);
            writer.Write(_worstThreshHole);
            writer.Write(_worstTime);
            writer.Write(_totalRunCount);
            writer.Write(_totalTime);
            writer.Write(_worstOccurance);
            writer.Write(_avgCummulativeOperations);
            
        }

        #endregion
    }
}