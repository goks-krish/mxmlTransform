using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;

namespace mxmlTransform.Mxml.Util
{
    /// <summary>
    /// Summary description for MxmlDocument.
    /// </summary>
    class MxmlDocument : XmlDocument
    {
        /// <summary>
        /// MxmlDocument Constructor.
        /// </summary>
        public MxmlDocument() : base()
		{}

        /// <summary>
        /// Formats the specified mxml file.
        /// </summary>
        /// <param name="mxmlFolderPath">Path to mxml file</param>
        public void Format(string mxmlFolderPath)
        {
            // collect and loop through all title nodes
            XmlNodeList titleNodeList = this.SelectNodes("descendant::title");
            foreach (XmlNode node in titleNodeList)
            {
                // Format title content
                node.InnerXml = this.FormatContent(node.InnerXml);
                // Format search-class attribute content
                if(node.Attributes["search-class"] != null)
                    node.Attributes["search-class"].InnerXml = this.FormatContent(node.Attributes["search-class"].InnerXml);
            }
            // ret�rn memory
            titleNodeList = null;

            // collect and loop through all paragraph nodes
            XmlNodeList paraNodeList = this.SelectNodes("descendant::p");
            foreach (XmlNode node in paraNodeList)
            {
                // Format paragraph content
                node.InnerXml = this.FormatContent(node.InnerXml);
            }
            // ret�rn memory
            paraNodeList = null;

            // collect and loop through all image nodes
            XmlNodeList imgNodeList = this.SelectNodes("descendant::img");
            foreach (XmlNode node in imgNodeList)
            {
                if (node.Attributes["type"] == null || node.Attributes["type"].InnerXml.Equals("normal"))
                {
                    // set path to image
                    string imagePath = mxmlFolderPath + node.Attributes["src"].InnerXml;

                    // get image information
                    FileInfo image = new FileInfo(imagePath);
                    
                    // initialize image width and height attributes
                    XmlAttribute widthAttribute = node.OwnerDocument.CreateAttribute("width");
                    XmlAttribute heightAttribute = node.OwnerDocument.CreateAttribute("height");

                    // if image exist
                    if (image.Exists)
                    {
                        // set width and heigth attributes 
                       widthAttribute.Value = System.Drawing.Image.FromFile(imagePath).Width.ToString();
                       heightAttribute.Value = System.Drawing.Image.FromFile(imagePath).Height.ToString();
                    }
                    else
                    {
                        // if not set width and height to 0
                        widthAttribute.Value = "0";
                        heightAttribute.Value = "0";
                    }

                    // append attributes to image node
                    node.Attributes.Append(widthAttribute);
                    node.Attributes.Append(heightAttribute);

                    // return memory
                    image = null;
                    widthAttribute = null;
                    heightAttribute = null;
                    imagePath = null;
                }
            }
            // return memory
            imgNodeList = null;

            // collect and loop through all symbol nodes
            XmlNodeList symbolNodeList = this.SelectNodes("descendant::symbol");
            foreach (XmlNode node in symbolNodeList)
            {
                // set path to symbol
                string symbolPath = mxmlFolderPath + node.Attributes["src"].InnerXml;

                // get symbol information
                FileInfo symbol = new FileInfo(symbolPath);

                // initialize symbol width and height attributes
                XmlAttribute widthAttribute = node.OwnerDocument.CreateAttribute("width");
                XmlAttribute heightAttribute = node.OwnerDocument.CreateAttribute("height");

                // if symbol exist
                if (symbol.Exists)
                {
                    // set width and heigth attributes
                    widthAttribute.Value = System.Drawing.Image.FromFile(symbolPath).Width.ToString();
                    heightAttribute.Value = System.Drawing.Image.FromFile(symbolPath).Height.ToString();
                }
                else
                {
                    // if not set width and height to 0
                    widthAttribute.Value = "0";
                    heightAttribute.Value = "0";
                }

                // append attributes to symbol node
                node.Attributes.Append(widthAttribute);
                node.Attributes.Append(heightAttribute);

                // return memory
                symbol = null;
                widthAttribute = null;
                heightAttribute = null;
                symbolPath = null;
            }
            // return memory
            symbolNodeList = null;
        }

        

        /// <summary>
        /// Formats the content specified content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Formated content</returns>
        private string FormatContent(string content)
        {
            
            // Format single tags.
            //bold
            content = this.FormatTag(content, "b");
            //italic
            content = this.FormatTag(content, "i");
            //underline
            content = this.FormatTag(content, "u");
            //sub
            content = this.FormatTag(content, "sub");
            //sup
            content = this.FormatTag(content, "sup");
            //em
            content = this.FormatTag(content, "em");


            // Format combined tags.
            //bold italic
            content = this.FormatCombinedTags(content, "b", "i");
            //bold underline
            content = this.FormatCombinedTags(content, "b", "u");
            //italic underline
            content = this.FormatCombinedTags(content, "i", "u");


            // :., rule
            Regex exp = new Regex("\\s+:\\s*");
            content = exp.Replace(content, ": ");
            exp = new Regex("\\s+,\\s*");
            content = exp.Replace(content, ", ");
            
            //br rules.
            /*exp = new Regex("\\s*(<br />)+\\s*");
            content = exp.Replace(content, "<br />");*/
            /*exp = new Regex("(<br />){2,}");
            content = exp.Replace(content, "<br />");*/
            exp = new Regex("^(<br />)");
            content = exp.Replace(content, "");
            exp = new Regex("(<br />)$");
            content = exp.Replace(content, "");

            //two or more spaces.
            exp = new Regex("\\s{2,}");
            content = exp.Replace(content, " ");

            //finally trim spaces.
            content = content.Trim();

            //symbol rules
            string tempContent = content;
            // remove end elements from temp content
            exp = new Regex("</[^>]*>");
            tempContent = exp.Replace(tempContent, "");
            // remove start elements from temp content
            exp = new Regex("<(b|i|u|sub|sup|em|xref|fref|br|lc|uc|hr)[^>]*>");
            tempContent = exp.Replace(tempContent, "");
            
            // dose temporary content end with symbol, add extra space to content
            if (tempContent.EndsWith("type=\"image\" />"))
                content = content + " ";

            // return memory
            exp = null;
            tempContent = null;

            return content;
        }

        /// <summary>
        /// Formats the specified xml tag.
        /// </summary>
        /// <param name="content">Content to format</param>
        /// <param name="tag">Tag to format</param>
        /// <returns>Formated content</returns>
        private string FormatTag(string content, string tag)
        {
            // move whitepaces outside element
            Regex exp = new Regex("<" + tag + ">\\s+");
            content = exp.Replace(content, " <" + tag + ">");

            // remove whitespaces between start elements
            exp = new Regex("<" + tag + ">\\s+<" + tag + ">");
            content = exp.Replace(content, "<" + tag + "><" + tag + ">");

            // move whitepaces outside element
            exp = new Regex("\\s+</" + tag + ">");
            content = exp.Replace(content, "</" + tag + "> ");

            // remove whitespaces between end elements
            exp = new Regex("</" + tag + ">\\s+</" + tag + ">");
            content = exp.Replace(content, "</" + tag + "></" + tag + ">");

            // return memory
            exp = null;

            return content;
        }

        /// <summary>
        /// Formats two combined xml tags.
        /// </summary>
        /// <param name="content">Content to format</param>
        /// <param name="tag1">First tag</param>
        /// <param name="tag2">Second tag</param>
        /// <returns>Formated content</returns>
        private string FormatCombinedTags(string content, string tag1, string tag2)
        {
            // remove whitespaces between combined elements
            Regex exp = new Regex("<" + tag1 + ">\\s+<" + tag2 + ">");
            content = exp.Replace(content, "<" + tag1 + "><" + tag2 + ">");

            // remove whitespaces between combined elements
            exp = new Regex("<" + tag2 + ">\\s+<" + tag1 + ">");
            content = exp.Replace(content, "<" + tag2 + "><" + tag1 + ">");

            // remove whitespaces between combined elements
            exp = new Regex("</" + tag1 + ">\\s+</" + tag2 + ">");
            content = exp.Replace(content, "</" + tag1 + "></" + tag2 + ">");

            // remove whitespaces between combined elements
            exp = new Regex("</" + tag2 + ">\\s+</" + tag1 + ">");
            content = exp.Replace(content, "</" + tag2 + "></" + tag1 + ">");

            // return memory
            exp = null;

            return content;
        }
    }
}
