using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;


namespace mxmlTransform.Mxml.Util
{
	/// <summary>
	/// Summary description for HtmlTextWriter.
	/// </summary>
	public class HtmlTextWriter : XmlTextWriter
	{
        private string[] fullEndElements = new string[] { "script", "link" };

        private string[] noEndElements = new string[] { "br", "hr" };

        private string lastStartElement = null;

        /// <summary>
        /// HtmlTextWriter Constructor.
        /// </summary>
        /// <param name="path">Path to mxml file</param>
        /// <param name="enc">Encoding to use</param>
		public HtmlTextWriter(string path, Encoding enc) : base(path, enc){}

        /// <summary>
        /// HtmlTextWriter Constructor.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
		public HtmlTextWriter(TextWriter writer):base(writer) {}

        /// <summary>
        /// HtmlTextWriter Constructor.
        /// </summary>
        /// <param name="writer">Stream to write to</param>
        /// <param name="enc">Encoding to use</param>
		public HtmlTextWriter(Stream writer, Encoding encoding):base(writer, encoding) {}


        /// <summary>
        /// Writes the specified string.
        /// </summary>
        /// <param name="text">text string to write</param>
		public override void WriteString(string text)
		{
            // replace some character in the string
			text = text.Replace("\x0D\x0A", "");
			text = text.Replace("\x20\x20", " ");
			text = text.Replace("\x09", "");

            // loop through characters in the text string
            for (int i = 0; i < text.Length; i++)
            {
                // get current character
                int utf = Convert.ToInt32(text[i]);

                // if character is in a certain range, convert to unicode entity
                if (utf > 127 || utf == 34 || utf == 39 || utf == 60 || utf == 62)
                    base.WriteRaw("&#" + utf.ToString() + ";");
                else
                    base.WriteRaw(text[i].ToString());
            }
		}

        /// <summary>
        /// Writes the specified start element.
        /// </summary>
        /// <param name="prefix">prefix of element</param>
        /// <param name="localName">local name of element</param>
        /// <param name="ns">name space of element</param>
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            // assign last start element to this element
            lastStartElement = localName;

            if (Array.IndexOf(noEndElements, lastStartElement) > -1)
            {
                base.WriteRaw("<" + localName + ">");
            }
            else
                base.WriteStartElement(prefix, localName, ns);

        }

        /// <summary>
        /// Writes the end element.
        /// </summary>
        public override void WriteEndElement()
        {
            //if the last opened element is in the no end elements array, then skip
            if (Array.IndexOf(noEndElements, lastStartElement) > -1)
            {
                lastStartElement = null;
            }
            //if the last opened element is in the full end elements array, then write a full end element for it
            else if (Array.IndexOf(fullEndElements, lastStartElement) > -1)
            {
                WriteFullEndElement();
            }
            else
            {
                base.WriteEndElement();
            }

        }

        

	}
}
