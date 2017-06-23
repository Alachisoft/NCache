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
using System.Text;
using System.Collections;

// C# Translation of GoldParser, by Marcus Klimstra <klimstra@home.nl>.
// Based on GOLDParser by Devin Cook <http://www.devincook.com/goldparser>.
namespace Alachisoft.NCache.Parser
{
	/// <summary>This class is used to read information stored in the very simple file
	/// structure used by the Compiled Grammar Table file.</summary>
	internal class GrammarReader
	{
		private const String	c_filetype = "GOLD Parser Tables/v1.0";

		private Encoding		m_encoding;
		private BinaryReader	m_reader;
		private Queue			m_entryQueue;
		
		/* constructor */
		
		public GrammarReader(String p_filename)
		{
			try
			{
				m_encoding = new UnicodeEncoding(false, true);
				m_reader = new BinaryReader(new FileStream(p_filename, FileMode.Open));
				m_entryQueue = new Queue();
			}
			catch (Exception e)
			{
				throw new ParserException("Error constructing GrammarReader", e);
			}

			if (! HasValidHeader())
				throw new ParserException("Incorrect file header");
		}

		public GrammarReader(Stream stream)
		{
			try
			{
				m_encoding = new UnicodeEncoding(false, true);
				m_reader = new BinaryReader(stream);
				m_entryQueue = new Queue();
			}
			catch (Exception e)
			{
				throw new ParserException("Error constructing GrammarReader", e);
			}

			if (! HasValidHeader())
				throw new ParserException("Incorrect file header");
		}
		
		/* public methods */
		
		public bool MoveNext()
		{
			try
			{
				EntryContent content = (EntryContent)m_reader.ReadByte();
				if (content == EntryContent.Multi)
				{
					m_entryQueue.Clear();
					int count = m_reader.ReadInt16();

					for (int n = 0; n < count; n++)
						ReadEntry();
						
					return true;
				}
				else
					return false;
			}
			catch (IOException)
			{
				return false;
			}
		}
		
		public Object RetrieveNext()
		{
			if (m_entryQueue.Count == 0)				
				return null;
			else
				return m_entryQueue.Dequeue();
		}
		
		public bool RetrieveDone()
		{
			return (m_entryQueue.Count == 0);
		}

		/* private methods */
		
		private bool HasValidHeader()
		{
			String filetype = ReadString();
			return filetype.Equals(c_filetype);
		}

		private String ReadString()
		{
			int pos = 0;
			byte[] buffer = new byte[1024];

			while (true)
			{
				m_reader.Read(buffer, pos, 2);
				if (buffer[pos] == 0) break;
				pos = pos + 2;
			}
			
			return m_encoding.GetString(buffer, 0, pos);
		}
		
		private void ReadEntry()
		{
			EntryContent content = (EntryContent)m_reader.ReadByte();
			switch (content)
			{
			case EntryContent.Empty:
				m_entryQueue.Enqueue(new Object());
				break;
			case EntryContent.Boolean:
				bool boolvalue = (m_reader.ReadByte() == 1);
				m_entryQueue.Enqueue(boolvalue);
				break;
			case EntryContent.Byte:
				byte bytevalue = m_reader.ReadByte();
				m_entryQueue.Enqueue(bytevalue);
				break;
			case EntryContent.Integer:
				Int16 intvalue = m_reader.ReadInt16();
				m_entryQueue.Enqueue(intvalue);
				break;
			case EntryContent.String:
				String strvalue = ReadString();
				m_entryQueue.Enqueue(strvalue);
				break;
			default:
				throw new ParserException("Error reading CGT: unknown entry-content type");
			}
		}
		
	}
}
