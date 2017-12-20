using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;

namespace mxmlTransform.Mxml.Util
{
	/// <summary>
	/// Summary description for MxmlTextWriter.
	/// </summary>
	public class MxmlTextWriter : XmlTextWriter
	{
        private Stack elements = new Stack();
        private Boolean disableComments;
        private String previouseString = "";

        /// <summary>
        /// MxmlDocument Constructor.
        /// </summary>
        /// <param name="path">Path to mxml file</param>
        /// <param name="enc">Encoding to use</param>
        /// <param name="disableComments">if set to <c>true</c> disable comments.</param>
		public MxmlTextWriter(string path, Encoding enc, Boolean disableComments) : base(path, enc)
		{
            this.disableComments = disableComments;
        }

        /// <summary>
        /// Writes the specified string.
        /// </summary>
        /// <param name="text">text string to write</param>
		public override void WriteString(string text)
		{
            //if string contains a '&' and its not part of a unicode entity, replace it with a unicode entity &#38;
			Regex regex = new Regex("&(?!#)");
			text = regex.Replace(text, "&#38;");

            // if string contains a capital X and it is part of a unicode entity, replace it with a lower case x 
			if(this.previouseString.Equals("#") && text.StartsWith("X"))
                text = text.Replace("X", "x");

            // loop through characters in the text string
			for(int i=0;i<text.Length;i++)
			{	
                // get current character
				int utf = Convert.ToInt32(text[i]);

                // if not a attribute string
                if (this.WriteState != WriteState.Attribute)
                {
                    // replace \n with a <br/>
                    if ((i != (text.Length - 1)) && ((text[i] == '\\') && (text[i + 1] == 'n')))
                    {
                        base.WriteRaw("<br/>");
                        i = i + 2;
                    }
                }

                // replace some unicode characters in the string
                if (utf == 64257)
                    base.WriteRaw("fi");
                else if (utf == 64258)
                    base.WriteRaw("fl");
                else if (utf == 145)
                    base.WriteRaw("&#8216;");
                else if (utf == 146)
                    base.WriteRaw("&#8217;");
                else if (utf == 147)
                    base.WriteRaw("&#8220;");
                else if (utf == 148)
                    base.WriteRaw("&#8221;");
                else if (utf == 149)
                    base.WriteRaw("&#8226;");
                else if (utf == 150)
                    base.WriteRaw("&#8211;");
                else if (utf == 151)
                    base.WriteRaw("&#8212;");
                else if (utf == 152)
                    base.WriteRaw("&#732;");
                else if (utf == 153)
                    base.WriteRaw("&#8482;");
                else if (utf == 611)
                    base.WriteRaw("&#947;");
                else if(utf > 127 || utf == 34 || utf == 39 || utf == 60 || utf == 62)
                    base.WriteRaw("&#" + utf.ToString() + ";");
				else
					base.WriteRaw(text[i].ToString());
			}

            // assign previosuse processed string as text
            this.previouseString = text;

            // return memory
            regex = null;
		}

        /// <summary>
        /// Writes the specified xml comment.
        /// </summary>
        /// <param name="text">comment to write</param>
		public override void WriteComment(string text)
		{
            // are comments enabled
		    if(!this.disableComments)
            {
                text = text.Replace("--", "&#45;&#45;");
                base.WriteComment(text);
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
            base.WriteStartElement(prefix, localName, ns);

            // if element is a paragraph, disable indentation
            if (localName.Equals("p") || localName.Equals("title"))
                this.Formatting = Formatting.None;

            // push element to element stack
            this.elements.Push(localName);
		}

        /// <summary>
        /// Writes the end element.
        /// </summary>
        public override void WriteEndElement()
        {
            // pop element from element stack
            string localName = elements.Pop() as string;

            base.WriteEndElement();

            // if element is a paragraph, enable indentation
            if (localName.Equals("p") || localName.Equals("title"))
                this.Formatting = Formatting.Indented;
        }


	}
}
