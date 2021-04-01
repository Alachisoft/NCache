
using System;
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{

    /// <summary>
    /// The class performs token processing in strings
    /// </summary>
    /// <summary>
    /// This class breaks a string into set of tokens and returns them one by one
    /// </summary>
    ///  Originally this class was written by someone else which highly
    /// relied upon use of exceptions for its functionality and since it is used
    /// in many places in the code it could affect the performance of NCache. 
    /// I have been asked to fix this performance bottleneck so I will rewrite this class.
    /// 
    /// Design of this class is totally useless but I'm going to follow the old design
    /// for the sake of compatibility of rest of the code.
    /// 
    /// Design flaws:
    /// -------------
    /// 1) HasMoreTokens() works same as MoveNext
    /// 2) MoveNext() internally calls HasMoreTokens
    /// 3) Current calls NextToken
    /// 4) NextToken() gives the current token
    /// 5) Count gives the number of remaining tokens
    //internal class Tokenizer : IEnumerator
    public class Tokenizer : IEnumerator
    {
        string text;
        char[] delims;
        string[] tokens;
        int index;

        public Tokenizer(string text, string delimiters)
        {
            this.text = text;
            delims = delimiters.ToCharArray();

            /// We do not need this function in 1x so contional compiling it
            /// reason: StringSplitOptions.RemoveEmptyEntries is not defined in system assembly of .net 1x

            tokens = text.Split(delims, StringSplitOptions.RemoveEmptyEntries);

            index = -1; // First call of MoveNext will put the pointer on right position.
        }

        public string NextToken()
        {
            return tokens[index]; // this is absurd
        }

        /// <summary>
        /// Remaining tokens count
        /// </summary>
        public int Count // bad design
        {
            get
            {
                if (index < tokens.Length)
                    return tokens.Length - index - 1;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Determines if there are more tokens to return from text.
        /// Also moves the pointer to next token
        /// </summary>
        /// <returns>True if there are more tokens otherwise, false</returns>
        public bool HasMoreTokens() //Hasan: bad design
        {
            if (index < tokens.Length - 1)
            {
                index++;
                return true;
            }
            else
                return false;
        }
        #region IEnumerator Members

        /// <summary>
        /// Performs the same action as NextToken
        /// </summary>
        public object Current
        {
            get { return NextToken(); }
        }

        /// <summary>
        /// Performs the same function as HasMoreTokens
        /// </summary>
        /// <returns>True if there are more tokens otherwise, false</returns>
        public bool MoveNext()
        {
            return HasMoreTokens(); //Hasan: this is absurd
        }

        public void Reset()
        {
            index = -1;
        }

        #endregion
    }
}

