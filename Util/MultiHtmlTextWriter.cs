using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;    
using System.IO;
using System.Security;
using System.ComponentModel;

namespace mxmlTransform.Mxml.Util
{
    /// <summary>
    /// Specifies the redirecting state of the <c>MultiXmlTextWriter</c>.
    /// </summary>
    internal enum RedirectState {Relaying, Redirecting, WritingRedirectElementAttrs, WritingRedirectElementAttrValue}

    /// <summary>
    /// <para><c>MultiXmlTextWriter</c> class extends standard <see cref="XmlTextWriter"/> class 
    /// and represents an XML writer that provides a fast, 
    /// non-cached, forward-only way of generating multiple output files containing
    /// either text data or XML data that conforms to the W3C Extensible Markup 
    /// Language (XML) 1.0 and the Namespaces in XML recommendations.</para>	
    /// </summary>
	public class MultiHtmlTextWriter : HtmlTextWriter {
	   
	    protected const string RedirectNamespace = "http://exslt.org/common";
	    protected const string RedirectElementName = "document";
	    	    
	    // Stack of output states
	    Stack states = null;	    
	    // Current output state
	    OutputState state = null;		        	    
	    // Currently processed attribute name 
	    string currentAttributeName;

        int numberOfPages = 0;
        int pageCounter = 0;
	    	    	    	    
	    //Redirecting state - relaying by default
	    RedirectState redirectState = RedirectState.Relaying;

        /// <summary>
        /// MultiHtmlTextWriter Constructor.
        /// </summary>
        /// <param name="fileName">ihtml file to write</param>
        /// <param name="enc">Encoding to use</param>	
        /// <param name="numberOfPages">Total number of iHtml pages to write</param>
        public MultiHtmlTextWriter(String fileName, Encoding enc, int numberOfPages) : base(fileName, enc) {
            // Set current working path
            DirectoryInfo dir = Directory.GetParent(fileName);
            Directory.SetCurrentDirectory(dir.ToString());

            this.numberOfPages = numberOfPages;
        }

        /// <summary>
        /// Checks possible start of <c>&lt;exsl:document></c> element content.         
        /// </summary>
        /// <remarks>
        /// When <c>&lt;exsl:document></c> element start tag is detected, the beginning of the 
        /// element's content might be detected as any next character data (not attribute
        /// value though), element start tag, processing instruction or comment.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <c>href</c> attribute is absent.</exception>
        /// <exception cref="ArgumentException">Thrown when a document, specified by <c>href</c> attribute is
        /// opened alreary. Two nested <c>&lt;exsl:document></c></exception> elements cannot specify the same 
        /// output URI in their <c>href</c> attributes.
        private void CheckContentStart() {
            if (redirectState == RedirectState.WritingRedirectElementAttrs) {
                //Check required href attribute
                if (state.Href == null)
                    throw new ArgumentNullException("'href' attribute of exsl:document element must be specified.");            
                //Are we writing to this URI already?
                foreach (OutputState nestedState in states)
                    if (nestedState.Href == state.Href)
                        throw new ArgumentException("Cannot write to " + state.Href + " two documents simultaneously.");                
                state.InitWriter();                                
                redirectState = RedirectState.Redirecting;
            }
        }

        /// <summary>
        /// Writes the specified start tag and associates it with the given namespace and prefix.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteStartElement"/>
        /// Overridden to detect <c>exsl:document</c> element start tag.
        /// </summary>        
        /// <param name="prefix">The namespace prefix of the element.</param>
        /// <param name="localName">The local name of the element.</param>
        /// <param name="ns">The namespace URI to associate with the element. If this namespace 
        /// is already in scope and has an associated prefix then the writer will automatically write that prefix also. </param>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        public override void WriteStartElement(string prefix, string localName, string ns) {        
            
            CheckContentStart();                            
            
            //Is it exsl:document redirecting instruction?
            if (localName == "html")
            {
                int procent = (int)((float)pageCounter++ / (float)this.numberOfPages * 100);
                // Console.writeLine(procent);
            }

            if (ns == RedirectNamespace && localName == RedirectElementName) {                
                //Lazy stack of states
                if (states == null)
                    states = new Stack();
                //If we are redirecting already - push the current state into the stack
                if (redirectState == RedirectState.Redirecting)
                    states.Push(state);
                //Initialize new state
                state = new OutputState();
                redirectState = RedirectState.WritingRedirectElementAttrs;
            } else {                            
                if (redirectState == RedirectState.Redirecting) {
                    if (state.Method == OutputMethod.Text) {
                        state.Depth++;
                        return;
                    }   
                    //Write doctype before the first element
                    if (state.Depth == 0 && state.SystemDoctype != null)
                        if (prefix != String.Empty)
                            state.XmlWriter.WriteDocType(prefix+":"+localName, 
                                state.PublicDoctype,state.SystemDoctype, null);
                        else
                            state.XmlWriter.WriteDocType(localName, 
                                state.PublicDoctype,state.SystemDoctype, null);
                    state.XmlWriter.Formatting = Formatting.None;
                    state.XmlWriter.WriteStartElement(prefix, localName, ns);                
                    state.Depth++;
                } else
                    base.WriteStartElement(prefix, localName, ns);              
            }
        }

        /// <summary>
        /// Finishes output redirecting - closes current writer 
        /// and pops previous state.
        /// </summary>
        internal void FinishRedirecting() {            
            state.CloseWriter();
            //Pop previous state if it exists
            if (states.Count != 0) {
                state = (OutputState)states.Pop();
                redirectState = RedirectState.Redirecting;
            } else {
                state = null;
                redirectState = RedirectState.Relaying;
            }
        }

        /// <summary>
        /// Closes one element and pops the corresponding namespace scope.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteEndElement"/>
        /// Overridden to detect <c>exsl:document</c> element end tag.
        /// </summary>    
        public override void WriteEndElement() {
            CheckContentStart();
            if (redirectState == RedirectState.Redirecting) {                
                //Check if that's exsl:document end tag
                if (state.Depth-- == 0)
                    FinishRedirecting();    
                else {
                    if (state.Method == OutputMethod.Text)
                        return;
                    state.XmlWriter.WriteEndElement();                         
                }
            } 
            else 
                base.WriteEndElement();
        }

        /// <summary>
        /// Closes one element and pops the corresponding namespace scope.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteFullEndElement"/>
        /// Overridden to detect <c>exsl:document</c> element end tag.
        /// </summary>   
        public override void WriteFullEndElement() {
            CheckContentStart();
            if (redirectState == RedirectState.Redirecting) {                
                //Check if it's exsl:document end tag
                if (state.Depth-- == 0)                                        
                    FinishRedirecting();               
                else {
                    if (state.Method == OutputMethod.Text)
                        return;
                    state.XmlWriter.WriteFullEndElement();                         
                }
            } else 
                base.WriteFullEndElement();                
        }

        /// <summary>
        /// Writes the start of an attribute.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteStartAttribute"/>
        /// Overridden to detect <c>exsl:document</c> attribute names and to redirect
        /// the output.
        /// </summary>
        /// <param name="prefix">Namespace prefix of the attribute.</param>
        /// <param name="localName">Local name of the attribute.</param>
        /// <param name="ns">Namespace URI of the attribute.</param>                                            
        /// <exception cref="ArgumentException"><c>localName</c>c> is either a null reference or <c>String.Empty</c>.</exception>
        public override void WriteStartAttribute(string prefix, string localName, string ns) {
         
            if (redirectState == RedirectState.WritingRedirectElementAttrs) {                                
                redirectState = RedirectState.WritingRedirectElementAttrValue;
                currentAttributeName = localName;                
            } else if (redirectState == RedirectState.Redirecting) {
                if (state.Method == OutputMethod.Text)
                    return;
                state.XmlWriter.WriteStartAttribute(prefix, localName, ns);                
            } else
                base.WriteStartAttribute(prefix, localName, ns);
        }

        /// <summary>
        /// Closes the previous <c>WriteStartAttribute</c> call.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteEndAttribute"/>
        /// Overridden to redirect the output.
        /// </summary>     
        public override void WriteEndAttribute() {                         
            if (redirectState == RedirectState.WritingRedirectElementAttrValue)
                redirectState = RedirectState.WritingRedirectElementAttrs;
            else if (redirectState == RedirectState.Redirecting) {
                if (state.Method == OutputMethod.Text)
                    return;
                state.XmlWriter.WriteEndAttribute();
            } else
                base.WriteEndAttribute();
        }

        /// <summary>
        /// Writes out a comment &lt;!--...--> containing the specified text.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteComment"/>
        /// Overriden to redirect the output.
        /// </summary>
        /// <param name="text">Text to place inside the comment.</param>        
        /// <exception cref="ArgumentException">The text would result in a non-well formed XML document.</exception>
        /// <exception cref="InvalidOperationException">The <c>WriteState</c> is Closed.</exception>
        public override void WriteComment(string text) {            
            CheckContentStart();
            if (redirectState == RedirectState.Redirecting) {
                if (state.Method == OutputMethod.Text)
                    return;
                state.XmlWriter.WriteComment(text);
            } else
                base.WriteComment(text);
        }

        /// <summary>
        /// Writes out a processing instruction with a space between the name 
        /// and text as follows: &lt;?name text?>.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteProcessingInstruction"/>
        /// Overridden to redirect the output.
        /// </summary>
        /// <param name="name">Name of the processing instruction.</param>
        /// <param name="text">Text to include in the processing instruction.</param>        
        /// <exception cref="ArgumentException"><para>The text would result in a non-well formed XML document.</para> 
        /// <para><c>name</c> is either a null reference or <c>String.Empty</c>.</para>
        /// <para>This method is being used to create an XML declaration after 
        /// <c>WriteStartDocument</c> has already been called.</para></exception>
        public override void WriteProcessingInstruction(string name, string text) {
            CheckContentStart();
            if (redirectState == RedirectState.Redirecting) {
                if (state.Method == OutputMethod.Text)
                    return;
                state.XmlWriter.WriteProcessingInstruction(name, text);
            } else
                base.WriteProcessingInstruction(name, text);
        }

        // <summary>
        /// Writes the given text content.
        /// Inherited from <c>XmlTextWriter</c>, see <see cref="XmlTextWriter.WriteString"/>
        /// Overridden to detect <c>exsl:document</c> element attribute values and to 
        /// redirect the output.
        /// </summary>
        /// <param name="text">Text to write.</param>        
        /// <exception cref="ArgumentException">The text string contains an invalid surrogate pair.</exception>
        public override void WriteString(string text) {

            int start = 0;
            int end = 0;

            if (text.Contains("[%WARNING%"))
            {
                start = text.IndexOf("[%WARNING% message=") + 19;
                end = text.IndexOf(']', start);
                //Console.writeLine(-1, "WARNING" + Environment.NewLine + text.Substring(start, (end - start)) + Environment.NewLine + Environment.NewLine);
            }
           
            //Possible exsl:document's attribute value
            if (redirectState == RedirectState.WritingRedirectElementAttrValue) {
                switch (currentAttributeName) {
                    case "href":
                        state.Href = state.Href + text;
                        break;
                    case "method":
                        if (text == "text")
                            state.Method = OutputMethod.Text;
                        break;    
                    case "encoding":
                        try {
                            state.Encoding = Encoding.GetEncoding(text);
                        } catch (Exception) {}    
                        break;
                    case "indent":
                        if (text == "yes")
                            state.Indent = true;
                        break;
                    case "doctype-public":
                        state.PublicDoctype = text;
                        break;
                    case "doctype-system":
                        state.SystemDoctype = text;
                        break;    
                    case "standalone":
                        if (text == "yes")
                            state.Standalone = true;
                        break;
                    default:
                        break;    
                }                
                return;
            } else
                CheckContentStart();
            if (redirectState == RedirectState.Redirecting) {
				if (state.Method == OutputMethod.Text)
					state.TextWriter.Write(text);
				else
				{
					Regex regex = new Regex("&(?!#)");
					text = regex.Replace(text, "&#38;");

					text = text.Replace("&#X", "&#x");

					for(int i=0;i<text.Length;i++)
					{	
						int utf = Convert.ToInt32(text[i]);

                        if ((i != (text.Length - 1)) && ((text[i] == '\\') && (text[i + 1] == 'n')))
						{
							state.XmlWriter.WriteString("<br/>");
							i++;
						}
						else if(utf > 127 || utf == 34 || utf == 39 || utf == 60 || utf == 62)
							state.XmlWriter.WriteString("&#" + utf.ToString() + ";");
						else
							state.XmlWriter.WriteString(text[i].ToString());
					}
				}
            } else
                base.WriteString(text);
        }
    }                       
}
