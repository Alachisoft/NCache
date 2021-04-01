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

namespace Alachisoft.NCache.Common.Stats
{
    /// <summary>
    /// Class that is useful in capturing statistics.
    /// </summary>
    [Serializable]
    public class TimeStats : Runtime.Serialization.ICompactSerializable
	{
		/// <summary> Total number of samples collected for the statistics. </summary>
		private long		_runCount;
		/// <summary> Timestamp for the begining of a sample. </summary>
		private long		_lastStart;
		/// <summary> Timestamp for the end of a sample. </summary>
		private long		_lastStop;
		/// <summary> Total time spent in sampling, i.e., acrued sample time. </summary>
		private long		_totalTime;
		/// <summary> Best time interval mesaured during sampling. </summary>
		private long		_bestTime;
		/// <summary> Worst time interval mesaured during sampling. </summary>
		private long		_worstTime;
		/// <summary> Avg. time interval mesaured during sampling. </summary>
		private float		_avgTime;
        /// <summary> Total number of samples collected for the statistics. </summary>
        private long        _totalRunCount;

        private long        _worstThreshHole = long.MaxValue;

        private long        _worstOccurance;

		/// <summary>
		/// Constructor
		/// </summary>
		public TimeStats()
		{
			Reset();
		}

        /// <summary>
        /// Constructor
        /// </summary>
        public TimeStats(long worstThreshHoleValue)
        {
            Reset();
            _worstThreshHole = worstThreshHoleValue;
        }

		/// <summary>
		/// Returns the total numbre of runs in the statistics capture.
		/// </summary>
		public long Runs	
		{ 
			get { lock(this){ return _runCount; } } 
		}

        /// <summary>
        /// Gets or sets the threshhold value for worst case occurance count.
        /// </summary>
        public long WorstThreshHoldValue
        {
            get { return _worstThreshHole; }
            set { _worstThreshHole = value; }
        }

        /// <summary>
        /// Gets the number of total worst cases occured.
        /// </summary>
        public long TotalWorstCases
        {
            get { return _worstOccurance; }
        }

		/// <summary>
		/// Returns the total time iterval spent in sampling
		/// </summary>
		public long Total	
		{ 
			get { lock(this){ return _totalTime; } } 
		}

		/// <summary>
		/// Returns the time interval for the last sample
		/// </summary>
		public long Current 
		{ 
			get { lock(this){ return _lastStop - _lastStart; } } 
		}

		/// <summary>
		/// Returns the best time interval mesaured during sampling
		/// </summary>
		public long Best	
		{ 
			get { lock(this){ return _bestTime; } } 
		}

		/// <summary>
		/// Returns the avg. time interval mesaured during sampling
		/// </summary>
		public float Avg		
		{ 
			get { lock(this){ return _avgTime ; } } 
		}

		/// <summary>
		/// Returns the worst time interval mesaured during sampling
		/// </summary>
		public long Worst	
		{ 
			get { lock(this){ return _worstTime; } } 
		}

		/// <summary>
		/// Resets the statistics collected so far.
		/// </summary>
		public void Reset()
		{
			_runCount = 0;
			_totalTime = _bestTime = _worstTime = _worstOccurance = 0;
			_avgTime = 0;
		}

		/// <summary>
		/// Timestamps the start of a sampling interval.
		/// </summary>
		public void BeginSample()
		{
			_lastStart = (DateTime.Now.Ticks - 621355968000000000) / 10000;
		}

			
		/// <summary>
		/// Timestamps the end of interval and calculates the sample time
		/// </summary>
		public void EndSample()
		{
			lock(this)
			{
				_lastStop = (DateTime.Now.Ticks - 621355968000000000) / 10000;
				AddSampleTime(Current);
			}
		}

        /// <summary>
        /// Timestamp the end of interval and calculates the sample time for bulk operations
        /// </summary>
        /// <param name="runcount">number of operations in bulk</param>
        public void EndSample(int runcount)
        {
            lock (this)
            {
                _lastStop = (DateTime.Now.Ticks - 621355968000000000) / 10000;
                AddSampleTime(Current, runcount);
            }
        }
		/// <summary>
		/// Adds a specified sample time to the statistics and updates the run count
		/// </summary>
		/// <param name="time">sample time in milliseconds.</param>
		public void AddSampleTime(long time)
		{
			lock(this)
			{
				_runCount ++;
                _totalRunCount ++;
				if(_runCount == 1)
				{
					_avgTime = _totalTime = _bestTime = _worstTime = time;
				}
				else
				{
					_totalTime += time;
					if(time < _bestTime)	_bestTime = time;
					if(time > _worstTime)	_worstTime = time;
                    if (time > _worstThreshHole) _worstOccurance += 1;
					_avgTime = (float)_totalTime / _runCount;
				}
			}
		}

        /// <summary>
        /// Adds a specified sample time to the statistics and updates the run count
        /// </summary>
        /// <param name="time">sample time in milliseconds.</param>
        /// <param name="runcount"> num of runs in case of bulk operations
        public void AddSampleTime(long time, int runcount)
        {
            lock (this)
            {
                _runCount+= runcount;
                _totalRunCount+= runcount;
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
                    _avgTime = (float)_totalTime / _runCount;
                }
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
			lock(this)
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

        #region ICompact Serializable Members
        public void Deserialize(CompactReader reader)
        {
            _runCount = reader.ReadInt64();
            _lastStart = reader.ReadInt64();
            _lastStop = reader.ReadInt64();
            _totalTime = reader.ReadInt64();
            _bestTime = reader.ReadInt64();
            _worstTime = reader.ReadInt64();
            _avgTime = (float)reader.ReadDouble();
            _totalRunCount = reader.ReadInt64(); 
            _worstThreshHole = reader.ReadInt64();
            _worstOccurance = reader.ReadInt64();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_runCount);
            writer.Write(_lastStart);
            writer.Write(_lastStop);
            writer.Write(_totalTime);
            writer.Write(_bestTime);
            writer.Write(_worstTime);
            writer.Write(_avgTime);
            writer.Write(_totalRunCount);
            writer.Write(_worstThreshHole);
            writer.Write(_worstOccurance);

        } 
        #endregion
    }
}
