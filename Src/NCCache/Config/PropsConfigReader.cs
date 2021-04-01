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
using System.Text;
using System.Data;
using System.Collections;



using Alachisoft.NCache.Runtime.Exceptions;



namespace Alachisoft.NCache.Config
{
	/// <summary>
	/// Utility class to help parse properties from a property string and convert it into 
	/// a HashMap of properties, which is later used by various classes for configurations.
	/// </summary>
	public class PropsConfigReader : ConfigReader
	{
		/// <summary>
		/// Class that helps parse the property string.
		/// </summary>
		class Tokenizer
		{
			/// <summary> original text to be tokenized. </summary>
			private string		_text;
			/// <summary> current token value. </summary>
			private string		_token;
			/// <summary> indexer used in parsing. </summary>
			private int			_index = 0;

			public const int	EOF = -1;
			public const int	ASSIGN = 0;
			public const int	NEST = 1;
			public const int	UNNEST = 2;
			public const int	CONTINUE = 3;
			public const int	ID = 4;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="text">text to be tokenized</param>
			public Tokenizer(string text)
			{
				_text = text;
			}


			/// <summary>
			/// Current token value.
			/// </summary>
			public string TokenValue
			{
				get { return _token; }
			}


			/// <summary>
			/// returns an identifier token value.
			/// </summary>
			/// <returns></returns>
			private string getIdentifier()
			{
				const string offendingStr = "=();";
				StringBuilder returnVal = new StringBuilder();

				if(_text[_index] == '\'')
				{
					_index++;
					while(_index < _text.Length)
					{
						if(_text[_index] == '\'')
						{
							_index ++;
							return returnVal.ToString();
						}
						if(_text[_index] == '\r' || _text[_index] == '\n' || _text[_index] == '\t')
						{
							_index++;
							continue;
						}
						returnVal.Append(_text[_index++]);
					}
					return null;
				}

				while(_index < _text.Length)
				{
					if(_text[_index] == '\r' || _text[_index] == '\n' || _text[_index] == '\t')
					{
						_index++;
						continue;
					}
					if(offendingStr.IndexOf(_text[_index]) != -1)
					{
						return returnVal.ToString();
					}
					returnVal.Append(_text[_index++]);
				}
				return null;
			}
			

			/// <summary>
			/// parses and returns the next token in the string.
			/// </summary>
			/// <returns>next token in the string.</returns>
			public int getNextToken()
			{
				string trimStr = "=();";
				while(_index < _text.Length)
				{
					if(trimStr.IndexOf(_text[_index]) != -1)
					{
						_token = _text[_index].ToString();
						return trimStr.IndexOf(_text[_index++]);
					}
					if(_text[_index] == '\r' || _text[_index] == '\n' || _text[_index] == '\t' || _text[_index] == ' ')
					{
						_index++;
						continue;
					}
					_token = getIdentifier();
					if(_token != null) return ID;
				}
				return EOF;
			}
		}


		/// <summary> Property string specified. </summary>
		private string _propString;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="propString">property string</param>
		public PropsConfigReader(string propString)
		{
			_propString = propString;
		}

		/// <summary>
		/// property string.
		/// </summary>
		public string PropertyString
		{
			get { return _propString; }
		}

		/// <summary>
		/// returns the properties collection
		/// </summary>
		override public Hashtable Properties
		{
			get { return GetProperties(_propString); }
		}

		/// <summary>
		/// Returns an xml config from the current properties string.
		/// </summary>
		/// <returns>xml configuration string.</returns>
		public string ToPropertiesXml()
		{
			return ConfigReader.ToPropertiesXml(Properties);
		}

		/// <summary>
		/// enumeration used to maintain the state of the parser.
		/// </summary>
		enum State 
		{
			/// <summary> Expecting a keyword </summary>
			keyNeeded,
			/// <summary> Expecting a value for the keyword </summary>
			valNeeded
		}

		/// <summary>
		/// Responsible for parsing the specified property string and returning a 
		/// HashMap representation of the properties specified in it.
		/// </summary>
		private Hashtable GetProperties(string propString)
		{
            bool uppercaseFlag = false;
			Hashtable	properties = new Hashtable();
			Tokenizer	tokenizer = new Tokenizer(propString);

			string		key = "";
			int			nestingLevel = 0;
			State		state = State.keyNeeded;
			Stack		stack = new Stack();

            try
            {
                do
                {
                    int token = tokenizer.getNextToken();
                    switch (token)
                    {
                        case Tokenizer.EOF:
                            if (state != State.keyNeeded)
                            {
                                throw new ConfigurationException("Invalid EOF");
                            }
                            if (nestingLevel > 0)
                            {
                                throw new ConfigurationException("Invalid property string, un-matched paranthesis");
                            }
                            return properties;

                        case Tokenizer.UNNEST:
                            if (state != State.keyNeeded)
                            {
                                throw new ConfigurationException("Invalid property string, ) misplaced");
                            }
                            if (nestingLevel < 1)
                                throw new ConfigurationException("Invalid property string, ) unexpected");
                            if (uppercaseFlag)
                                uppercaseFlag = false;
                            properties = stack.Pop() as Hashtable;
                            nestingLevel--;
                            break;

                        case Tokenizer.ID:
                            switch (state)
                            {
                                case State.keyNeeded:
                                    if (key == "parameters")
                                        uppercaseFlag = true;
                                    key = tokenizer.TokenValue;
                                    token = tokenizer.getNextToken();

                                    if (token == Tokenizer.CONTINUE ||
                                        token == Tokenizer.UNNEST ||
                                        token == Tokenizer.ID ||
                                        token == Tokenizer.EOF)
                                    {
                                        throw new ConfigurationException("Invalid property string, key following a bad token");
                                    }

                                    if (token == Tokenizer.ASSIGN)
                                    {
                                        state = State.valNeeded;
                                    }
                                    else if (token == Tokenizer.NEST)
                                    {
                                        stack.Push(properties);
                                        properties[key.ToLower()] = new Hashtable();
                                        properties = properties[key.ToLower()] as Hashtable;

                                        state = State.keyNeeded;
                                        nestingLevel++;
                                    }
                                    break;

                                case State.valNeeded:
                                    string val = tokenizer.TokenValue;
                                    token = tokenizer.getNextToken();
                                    state = State.keyNeeded;

                                    if (token == Tokenizer.ASSIGN || token == Tokenizer.ID || token == Tokenizer.EOF)
                                    {
                                        throw new ConfigurationException("Invalid property string, value following a bad token");
                                    }

                                    if (uppercaseFlag)
                                        properties[key] = val;
                                    else
                                        properties[key.ToLower()] = val;

                                    if (token == Tokenizer.NEST)
                                    {
                                        stack.Push(properties);
                                        properties[key.ToLower()] = new Hashtable();
                                        properties = properties[key.ToLower()] as Hashtable;

                                        properties.Add("id", key);
                                        properties.Add("type", val);

                                        state = State.keyNeeded;
                                        nestingLevel++;
                                    }
                                    else if (token == Tokenizer.UNNEST)
                                    {
                                        if (nestingLevel < 1)
                                            throw new ConfigurationException("Invalid property string, ) unexpected");
                                        if (uppercaseFlag)
                                            uppercaseFlag = false;
                                        properties = stack.Pop() as Hashtable;
                                        nestingLevel--;
                                        state = State.keyNeeded;
                                    }
                                    break;
                            }
                            break;

                        default:
                            throw new ConfigurationException("Invalid property string");
                    }
                }
                while (true);
            }
            catch (Exception e)
            {

                throw;
            }
            return properties;
		}
	}
}
