// Gold Parser engine.
// See more details on http://www.devincook.com/goldparser/
// 
// Original code is written in VB by Devin Cook (GOLDParser@DevinCook.com)
//
// This translation is done by Vladimir Morozov (vmoroz@hotmail.com)
// 
// The translation is based on the other engine translations:
// Delphi engine by Alexandre Rai (riccio@gmx.at)
// C# engine by Marcus Klimstra (klimstra@home.nl)

using System;
using System.IO;

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// This is a wrapper around StreamReader which supports lookahead.
	internal class LookAheadReader
	{
		private const int BUFSIZE = 256;
        
		private TextReader	m_reader;
		private char[]			m_buffer;
		private int				m_curpos;
		private int				m_buflen;
		
		/* constructor */
		
		/// Creates a new LookAheadReader around the specified StreamReader.
		public LookAheadReader(TextReader p_reader)
		{
			m_reader = p_reader;
			m_curpos = -1;
			m_buffer = new char[BUFSIZE];			
		}
		
		/* private methods */

		/// Makes sure there are enough characters in the buffer.
		private void FillBuffer(int p_length)
		{
			int av = m_buflen - m_curpos;	// het aantal chars na curpos
			
			if (m_curpos == -1)
			{
				// fill the buffer
				m_buflen = m_reader.Read(m_buffer, 0, BUFSIZE);
				m_curpos = 0;
			}
			else if (av < p_length)
			{
				if (m_buflen < BUFSIZE)
					// not available
					throw new EndOfStreamException();
				else
				{
					// re-fill the buffer								
					Array.Copy(m_buffer, m_curpos, m_buffer, 0, av);
                    int read = m_reader.Read(m_buffer, av, m_curpos);
					m_buflen = read + av;
					m_curpos = 0;

                    //Fix for client issue regarding the length of the query
                    if (m_reader.Peek() == -1 && m_buflen == BUFSIZE && read == 0)
                        throw new EndOfStreamException();
				}
			}			
		
			// append a newline on EOF
			if (m_buflen < BUFSIZE)
				m_buffer[m_buflen++] = '\n';
		}
		
		/* public methods */
		
		/// Returns the next char in the buffer but doesn't advance the current position.
		public char LookAhead()
		{
			FillBuffer(1);
			return m_buffer[m_curpos];
		}
		
		/// <summary>Returns the char at current position + the specified number of characters.
		/// Does not change the current position.</summary>
		/// <param name="p_pos">The position after the current one where the character to return is</param>
		public char LookAhead(int p_pos)
		{
			FillBuffer(p_pos + 1);
			return m_buffer[m_curpos + p_pos];
		}
		
		/// Returns the next char in the buffer and advances the current position by one.
		public char Read()
		{
			FillBuffer(1);
			return m_buffer[m_curpos++];
		}
		
		/// Returns the next n characters in the buffer and advances the current position by n.
		public String Read(int p_length)
		{
			FillBuffer(p_length);
			String result = new String(m_buffer, m_curpos, p_length);
			m_curpos += p_length;
			return result;
		}
		
		/// Advances the current position in the buffer until a newline is encountered.
		public void DiscardLine()
		{
			while (LookAhead() != '\n')
				m_curpos++;
		}
		
		/// Closes the underlying StreamReader.
		public void Close()
		{
			m_reader.Close();
		}
	}
}
