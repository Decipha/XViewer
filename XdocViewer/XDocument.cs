using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Sharp.Gml;

namespace XdocViewer
{
	/// <summary>
	/// methods for reading binary xdocs.
	/// xdoc is not a proprietary format, it's just XML compressed using Gzip.
	/// </summary>
	class XDocument
	{
		/// <summary>
		/// parse the xml into generic-markup-language document nodes, then render the markup with appropriate indentation/formatting.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static string FormatXML(string xml, bool ignoreFormatError = false)
		{
            try
            {
                return GmlDocument.Parse(xml).Markup;
            }
            catch
            {
                if (ignoreFormatError)
                    return xml;
                else
                    throw;
            }
		}

        /// <summary>
        /// opens a binary xml document (.xdoc) and returns the unformatted XML string.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
		public static string ReadBinaryXDoc(string fileName)
		{
			using (var fs = File.OpenRead(fileName))
			{
				using (var ms = new MemoryStream())
				{
					using (var gzip = new GZipStream(fs, CompressionMode.Decompress))
					{
						gzip.CopyTo(ms);
						ms.Position = 0;

						using (var rdr = new StreamReader(ms))
						{
							return rdr.ReadToEnd();
						}
					}
				}
			}
		}

        /// <summary>
        /// opens a KTM project file as an XML string.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
		public static string ReadFPRFile(string fileName)
		{
			using (var fs = File.OpenRead(fileName))
			{
				var arc = new ZipArchive(fs, ZipArchiveMode.Read);
				foreach (var ntree in arc.Entries)
				{
					using (var rdr = new StreamReader(ntree.Open()))
					{
						return rdr.ReadToEnd();
					}
				}
			}
			return "";
		}

	}
}
