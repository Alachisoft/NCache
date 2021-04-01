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
using System.Diagnostics;

namespace Alachisoft.NCache.Common.Stats
{
    [Serializable]
    public class NanoSecTimeStats : Runtime.Serialization.ICompactSerializable
    {
        private long start;
        private long stop;
        private long frequency;
        Decimal multiplier = new Decimal(1.0e6);

        public NanoSecTimeStats()
        {
            frequency = Stopwatch.Frequency;
            if(!Stopwatch.IsHighResolution)
            {
                throw new Exception("frequency not supported");
            }
        }

        public void Start()
        {
            start = Stopwatch.GetTimestamp();
            if (start < 0)
                start = start * -1;
        }

        public void Stop()
        {
           stop = Stopwatch.GetTimestamp();
            if (stop < 0)
                stop = stop * -1;
        }

        /// <summary>
        /// returns the nanoseconds per iteration.
        /// </summary>
        /// <param name="iterations">total iterations.</param>
        /// <returns>Nanoseconds per Iteration.</returns>
        public double Duration(int iterations)
        {
            return ((((double)(stop - start) * (double)multiplier) / (double)frequency) / iterations);
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            start = reader.ReadInt64();
            stop = reader.ReadInt64();
            frequency = reader.ReadInt64();
            multiplier = reader.ReadDecimal();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(start);
            writer.Write(stop);
            writer.Write(frequency);
            writer.Write(multiplier);
        } 
        #endregion
    }
}