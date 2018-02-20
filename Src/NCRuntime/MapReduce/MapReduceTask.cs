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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// Initialize Mapper, Reducer, Combiner 
    /// </summary>
    [Serializable]
    public class MapReduceTask
    {
        private IMapper _mapper = null;
        private ICombinerFactory _combiner = null;
        private IReducerFactory _reducer = null;

        private int _mrOutputOption = 0;

        private MapReduceInput inputProvider = null;

        public MapReduceTask() { }

        public MapReduceTask(IMapper mapper, ICombinerFactory combinerFactory, IReducerFactory reducerFactory)
        {
            this.Mapper = mapper;
            this.Combiner = combinerFactory;
            this.Reducer = reducerFactory;

        }
        /// <summary>
        /// Set/Gets values of the Mapper
        /// </summary>
        public IMapper Mapper
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException("mapper");

                this._mapper = value;
            }
            get 
            {
                return _mapper;
            }
        }
        /// <summary>
        /// Set/Gets values of Combiner
        /// </summary>
        public ICombinerFactory Combiner
        {
            set
            {
                this._combiner = value;
            }
            get
            {
                return _combiner;
            }
        }
        /// <summary>
        /// Set/Gets Values of Reducer
        /// </summary>
        public IReducerFactory Reducer
        {
            set
            {
                this._reducer = value;
            }
            get
            {
                return _reducer;
            }
        }

        public MapReduceInput InputProvider
        {
            get { return inputProvider; }
            set { inputProvider = value; }
        }
    }
}
