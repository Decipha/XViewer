using Binary.Conversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XdocViewer.Hex
{
	class HexDocument
	{
		/// <summary>
		/// determines the length (in bytes) for each word
		/// </summary>
		public int WordSize   { get; set; } = 4;

		/// <summary>
		/// determines the number of words to display in a single row
		/// </summary>
		public int Columns	  { get; set; } = 8;

		/// <summary>
		/// calculates the byte length of each row
		/// </summary>
		public int RowLength  { get { return WordSize * Columns; } }

		/// <summary>
		/// specifies the selected word
		/// </summary>
		public int Caret { get; set; }

		/// <summary>
		/// calculates the offset at the current caret position
		/// </summary>
		public int Offset {  get { return Caret * WordSize; } }

		/// <summary>
		/// gets the word under the caret
		/// </summary>
		public Word Selected {  get { return Words[Caret]; } }

		/// <summary>
		/// estimate the word size in the specified file by checking 64,32,16 and 8 bit word sizes
		/// </summary>
		/// <param name="fileSize">the file-size in bytes</param>
		/// <returns>
		/// the best guess for word size in bytes.
		/// </returns>
		public static int EstimateWordSize(long fileSize)
		{
			if (fileSize % 8 == 0)
				return 8;
			if (fileSize % 4 == 0)
			    return 4;
			if (fileSize % 2 == 0)
				return 2;

			return 1;
		}

		/// <summary>
		/// collection of 'words' - each word representing a number of bytes
		/// this is specific to the word length.
		/// </summary>
		public List<Word> Words { get; } = new List<Word>();

		/// <summary>
		/// the encoding to use to convert bytes to text
		/// </summary>
		public Encoding TextEncoding { get; set; } = Encoding.UTF8;

		/// <summary>
		/// the raw data
		/// </summary>
		public IEnumerable<byte> Data
		{
			get
			{
				foreach (var wrd in Words)
				{
					foreach (var hb in wrd.HexBytes)
					{
						yield return hb.Value;
					}
				}
			}
		}

		/// <summary>
		/// the words returned in rows
		/// </summary>
		public IEnumerable<Row> Rows
		{
			get
			{
				long offset = 0;
				int start   = 0;
				int cols    = this.Columns;

				while (start < Words.Count)
				{
					// construct the row:
					var row = new Row(offset, start, cols, Words) { Encoding = this.TextEncoding };

					// update the pointers
					start  += cols;
					offset += row.ByteLen;

					// yield the row:
					yield return row;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		public HexDocument(Stream source)
		{
			while (true)
			{
				var word = new byte[WordSize];
				int read = source.Read(word, 0, WordSize);
				if (read > 0)
				{
					Words.Add(new Word(word, 0, read));
				}
				else
					break;
			}
		}


	}

	/// <summary>
	/// represents a row of binary words as would be found in a hex editor
	/// </summary>
	public class Row
	{

		public Row(long offset, int start, int cols, List<Word> words)
		{
			this.Offset = offset; this.Columns = cols;
			foreach (var word in words.Skip(start).Take(cols))
			{
				Words.Add(word);
				ByteLen += word.Length;
			}
		}

		public Row(Stream source, int cols, int wordLen)
		{
			this.Columns = cols;
			this.Offset = source.Position;
			this.ByteLen = cols * wordLen;
			while (source.Position - Offset < ByteLen)
			{
				var word = new byte[wordLen];
				int read = source.Read(word, 0, wordLen);
				if (read > 0)
				{
					Words.Add(new Word(word, 0, read));
				}

			}
		}

		/// <summary>
		/// specifies the number of words to show in the row
		/// </summary>
		public int Columns { get; set; }

		/// <summary>
		/// the file offset at the start of the row
		/// </summary>
		public long Offset { get; set; }

		/// <summary>
		/// the words in the row
		/// </summary>
		public List<Word> Words { get; } = new List<Word>();

		/// <summary>
		/// the number of bytes in the row
		/// </summary>
		public int ByteLen { get; set; }

		/// <summary>
		/// the text-encoding used to render the row as a string
		/// </summary>
		public Encoding Encoding { get; set; } = Encoding.UTF8;

		/// <summary>
		/// enumeration of the bytes in the row
		/// </summary>
		public IEnumerable<byte> Bytes
		{
			get
			{
				foreach (var w in Words)
					foreach (var b in w.HexBytes)
						yield return b.Value;
			}
		}

		/// <summary>
		/// the hex representation of the row start offset
		/// </summary>
		public string OffsetHex
		{
			get { return Convert.ToString(this.Offset, 16).PadLeft(10, '0'); }
		}

		/// <summary>
		/// the hex representation of the words in the row, tab seperated
		/// </summary>
		public string RowColumnsString
		{
			get
			{
				var sb = new StringBuilder();

				foreach (var wrd in Words)
				{
					if (sb.Length > 0)
						sb.Append("\t");
					sb.Append(wrd.ToString());
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// the string representation of the current row using the specified encoder.
		/// </summary>
		public string RowStringDecoded
		{
			get { return this.Encoding.GetString(Bytes.ToArray()); }
		}
	}

	/// <summary>
	/// represents a single word (1,2,4,8 bytes)
	/// </summary>
	public class Word
	{
		/// <summary>
		/// gets or sets the word length
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// list of bytes
		/// </summary>
		public List<HexByte> HexBytes { get; } = new List<HexByte>();

		/// <summary>
		/// reconstructs the original byte array that made the word.
		/// </summary>
		public byte[] Buffer
		{
			get { return (from h in HexBytes select h.Value).ToArray(); }
		}

		/// <summary>
		/// gets the word as a signed decimal value
		/// </summary>
		public long SignedDecimal
		{
			get { return BitConverter.ToInt64(Buffer, 0); }
		}

		#region Constructor

		/// <summary>
		/// constructor: take a word from the enumeration @start for length bytes
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		public Word(IEnumerable<byte> buffer, int start, int length)
		{
			foreach (byte b in buffer.Skip(start).Take(length))
			{
				Length++;
				HexBytes.Add(new HexByte(b));
			}
		}

		/// <summary>
		/// construct the word from the entire enumeration
		/// </summary>
		/// <param name="source"></param>
		public Word(IEnumerable<byte> source)
		{
			foreach (byte b in source)
			{
				Length++;
				HexBytes.Add(new HexByte(b));
			}
		}

		#endregion

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var hb in HexBytes) {
				if (sb.Length > 0)
					sb.Append(' ');
				sb.Append(hb.Hex);
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// represents a single byte
	/// </summary>
	public class HexByte
	{
		public HexByte(byte b)     { this.Value = b; }
		public HexByte(string hex) { this.Hex = hex; }

		/// <summary>
		/// gets or sets the decimal byte value
		/// </summary>
		public byte Value { get; set; }

		/// <summary>
		/// gets or sets the value of the byte as a hex string
		/// </summary>
		public string Hex
		{
			get
			{
				return Convert.ToString(Value, 16).PadLeft(2, '0');
			}
			set
			{
				Value = Convert.ToByte(value, 16);
			}
		}

		/// <summary>
		/// gets the value of the byte as a string of 1 and 0 
		/// </summary>
		public string BinaryString
		{
			get
			{
				var sb = new StringBuilder();
				foreach (var b in Bits)
				{
					sb.Append(b ? '1' : '0');
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// gets the byte as an array of 8 bits
		/// </summary>
		public bool[] Bits
		{
			get
			{
				var bits = new bool[8];
				for (int i = 0; i < 8; i++)
					bits[i] = BitMan.GetBit(Value, i);
				return bits;
			}
		}

		public override string ToString()
		{
			return $"0x{Hex}";
		}
	}

	public static class ExtensionMethods
	{
		public static string ToHexString(this byte[] bytes)
		{
			var sb = new StringBuilder();
			foreach (byte b in bytes)
			{
				if (sb.Length > 0)
					sb.Append(" ");
				sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
			}
			return sb.ToString();
		}
	}
}
