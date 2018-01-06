using System;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

using Mvp.Xml.Exslt;
using Mvp.Xml.Common.Xsl;

using mxmlTransform.Mxml.Util;

namespace mxmlTransform
{
    class Program
    {
        private static string commonSrc = null;
        private static string bookSrcPath = null;
        private static string outputTarPath = null;
        private static string mxmlFile = null;
        private static string unicodeFile = null;
        private static Boolean mxmlIsValid = true;
        private static string BookDir = null;
        private static string BookName = null;
        private static string BookSrc = null;
        private static string BookXml = null;
        private static string BookXslt = null;
        private static string BookXsltIthml = null;
        private static string BookMxml = null;
        private static string BookUnicode = null;

        static void Main(string[] args)
        {

            if(args.Length != 2) 
            {
                Console.WriteLine("Usage: mxmlTransfrom <input_folder> <output_folder>");
                throw new Exception("Sorry, provide input and output folders as arguements");
            }
            bookSrcPath = args[0];
            outputTarPath = args[1];
            
            if(!bookSrcPath.EndsWith("\\")) {
                bookSrcPath = bookSrcPath + "/";
            }
            if(!outputTarPath.EndsWith("\\")) {
                outputTarPath = outputTarPath + "/";
            }
            
            commonSrc =  new DirectoryInfo(bookSrcPath).Parent.FullName + "/common/";
            
            string[] DefineFiles = Directory.GetFiles(bookSrcPath+"define/","*define.xml",SearchOption.TopDirectoryOnly);
            
            //1. Initialize: Get data from define, Create Dirs and copy common files
            InitializeFromXML(DefineFiles[0]);
            createDirStructure();

            //2. MXML Transformation
            processMxmlTransform();

            //3. Validate
            validateMxml();

            //4. Collect Unicode
            collectUnicode();

            //5. IHTML Transformation
            deleteHtmlDir();
            copyRequiredData();
            processIhtmlTransform();
        }
        
        static void copyRequiredData() 
        {
            //1. Copy css from CommonFolder to ihtml
            DirectoryCopy(commonSrc+"ihtml/css", outputTarPath+"ihtml/css",true);
            
            //2. Copy script from CommonFolder to ihtml
            DirectoryCopy(commonSrc+"ihtml/script", outputTarPath+"ihtml/script",true);

            //3. Copy book.css to ihtml
            new FileInfo(bookSrcPath+"css/book.css").CopyTo(outputTarPath+"ihtml/css/book.css");
        }

        static void deleteHtmlDir() 
        {
            //1. Delete html dir
            deleteDirectory(outputTarPath + "ihtml/html");

            //2. Delete search dir
            deleteDirectory(outputTarPath + "ihtml/search");

            //3. Delete toc dir
            deleteDirectory(outputTarPath + "ihtml/toc");

            //4. Delete script dir
            deleteDirectory(outputTarPath + "ihtml/script");

            //5. Delete css dir
            deleteDirectory(outputTarPath + "ihtml/css");
        }

        static void createDirStructure() 
        {
            Console.WriteLine("Preparing Directories at " + DateTime.Now);
            //1. Create output directory
            createDirectory(outputTarPath);

            //2. Create mxml directory
            createDirectory(outputTarPath + "mxml");

            //3. Create ihtml directory
            createDirectory(outputTarPath + "ihtml");

            //4. Create unicode directory
            createDirectory(outputTarPath + "unicode");

            Console.WriteLine("Directories complete at " + DateTime.Now);            
        }

        static void deleteDirectory(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(dir));
            if (dirInfo.Exists)
            {
                Directory.Delete(dir,true);
            }
            dirInfo = null;
        }
        static void createDirectory(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetFullPath(dir));
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo = null;
        }
        
        static void deleteFile(string filePath) 
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            fileInfo = null;
        }

        static void processMxmlTransform() 
        {
            Console.WriteLine("Starting MXML Transformation at " + DateTime.Now);

            string commonPath = commonSrc + "mxml/xslt/";
            string inputPath = bookSrcPath + "/" + BookSrc + "/" + BookXml;
            string outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_BASIC.xml";
            string xsltPath = bookSrcPath + "xslt/" + BookXslt;
            Console.WriteLine("Applying MXML xslt at " + DateTime.Now);
            
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                xsltTransform(inputPath, outputPath, xsltPath);
            } 
            else 
            {
                try
                { 
                    String line = null;
                    using (StreamReader sr = new StreamReader(inputPath))
                    {
                        line = sr.ReadToEnd();
                    }
                    inputPath = bookSrcPath + "/" + BookSrc + "/manifest.xml";
                    line = line.Replace("\\","/");
                    using (StreamWriter outputFile = new StreamWriter(inputPath)) 
                    {     
                        outputFile.WriteLine(line);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                xsltTransform(inputPath, outputPath, xsltPath);
                deleteFile(inputPath);
            }

            inputPath = outputPath;
            xsltPath = commonPath + "table-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_TABLE.xml";
            Console.WriteLine("Applying Table xslt at " + DateTime.Now);
            xsltTransform(inputPath, outputPath, xsltPath);
            deleteFile(inputPath);

            inputPath = outputPath;
            xsltPath = commonPath + "image-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_IMAGE.xml";
            Console.WriteLine("Applying Image xslt at " + DateTime.Now);
            xsltTransform(inputPath, outputPath, xsltPath);
            deleteFile(inputPath);

            inputPath = outputPath;
            xsltPath = commonPath + "list-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_LIST.xml";
            Console.WriteLine("Applying List xslt at " + DateTime.Now);
            xsltTransform(inputPath, outputPath, xsltPath);
            deleteFile(inputPath);

            inputPath = outputPath;
            xsltPath = commonPath + "grouping-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_GROUPING.xml";
            Console.WriteLine("Applying List xslt at " + DateTime.Now);
            xsltTransform(inputPath, outputPath, xsltPath);
            deleteFile(inputPath);

            inputPath = outputPath;
            xsltPath = commonPath + "footnote-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_FOOTNOTE.xml";
            Console.WriteLine("Applying Footnote xslt at " + DateTime.Now);
            xsltTransform(inputPath, outputPath, xsltPath);
            deleteFile(inputPath);

            inputPath = outputPath;
            xsltPath = commonPath + "formating-mxml.xsl";
            outputPath = outputTarPath + "mxml/" + BookName + "_mxml_TEMP_DISABLECOMMENTS.xml";
            Console.WriteLine("Applying Disable comments xslt at " + DateTime.Now);
            xsltFormatTransform(inputPath, outputPath, xsltPath);  
            deleteFile(inputPath);

            inputPath = outputPath;
            Console.WriteLine("Writing to final MXML " + DateTime.Now);
            finalMxmlWriter(inputPath,mxmlFile);    
            deleteFile(inputPath);

            Console.WriteLine("Completed MXML Transformation at " + DateTime.Now);      
        } 

        static void xsltTransform(string inputPath, string outputPath, string xsltPath) 
        {
            XmlUrlResolver resolver = new XmlUrlResolver();
            XsltSettings xsltSettings = new XsltSettings(true, true);
            MxmlTextWriter mxmlWriter = mxmlWriter = new MxmlTextWriter(outputPath, null, true);
            XmlInput input = new XmlInput(inputPath);
            MvpXslTransform mxmlTransform = new MvpXslTransform();

            mxmlTransform.Load(xsltPath,xsltSettings, resolver);
            mxmlTransform.Transform(input, null, new XmlOutput(mxmlWriter));

            mxmlWriter.Flush();
            mxmlWriter.Close();
            mxmlWriter = null; 
        }

        static void xsltFormatTransform(string inputPath, string outputPath, string xsltPath) 
        {
            XmlUrlResolver resolver = new XmlUrlResolver();
            XsltSettings xsltSettings = new XsltSettings(true, true);
            MxmlTextWriter mxmlWriter = mxmlWriter = new MxmlTextWriter(outputPath, null, true);
            XmlInput input = new XmlInput(inputPath);
            MvpXslTransform mxmlTransform = new MvpXslTransform();

            mxmlWriter.Formatting = Formatting.Indented;
            mxmlWriter.IndentChar = '\t';
            mxmlWriter.Indentation = 1;
            mxmlWriter.WriteRaw("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            mxmlWriter.WriteRaw("<?xml-stylesheet type=\"text/xsl\" href=\"../../../material/css/"  + BookName +"/" +  BookName +".css\"?>\n"); 
            
            mxmlWriter.WriteRaw("<!DOCTYPE book SYSTEM \"" + commonSrc + "mxml/dtd/mxml.dtd\">\n");

            mxmlTransform.Load(xsltPath,xsltSettings, resolver);
            mxmlTransform.Transform(input, null, new XmlOutput(mxmlWriter));

            mxmlWriter.Flush();
            mxmlWriter.Close();
            mxmlWriter = null;
        }

        static void finalMxmlWriter(string inputPath, string outputPath) 
        {
            // write final output with disabled comments
            MxmlDocument doc = new MxmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(inputPath);
            doc.Format(outputTarPath + "mxml/" );
            MxmlTextWriter mxmlWriter = new MxmlTextWriter(outputPath, null, true);
            doc.WriteTo(mxmlWriter);

            mxmlWriter.Flush();
            mxmlWriter.Close();
            mxmlWriter = null; 
        }

        static void validateMxml() 
        {
            Console.WriteLine("Started MXML Validation at " + DateTime.Now);  

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationType = ValidationType.DTD;
            settings.IgnoreWhitespace = true;
            settings.XmlResolver = new XmlUrlResolver();
            settings.ValidationEventHandler += new ValidationEventHandler(MxmlValidationEventHandler);

            XmlReader mxmlReader = XmlReader.Create(mxmlFile, settings);

            while(mxmlReader.Read() && mxmlIsValid) {}
            if(mxmlIsValid) 
            {
                Console.WriteLine("Completed MXML Validation at " + DateTime.Now);  
            }
        }

        static void MxmlValidationEventHandler(object sender, ValidationEventArgs args)
		{
			mxmlIsValid = false;
            Console.WriteLine("Validation error: " + Environment.NewLine + args.Message + "(" + args.Exception.LineNumber.ToString() + "," + args.Exception.LinePosition.ToString() + ")" + Environment.NewLine);
        }

        static void collectUnicode() 
        {
            Console.WriteLine("Started Unicode Collection at " + DateTime.Now);
            XmlReader mxmlTextReader = new XmlTextReader(mxmlFile);  
            SortedList unicodeList = new SortedList();

            while (mxmlTextReader.Read())
            {
                foreach (char c in mxmlTextReader.ReadString().ToCharArray())
                {
                    if (!unicodeList.ContainsValue(c))
                    {
                        // add character to list
                        unicodeList.Add((int)c, c);
                    }
                }
            }
            
            IDictionaryEnumerator enumerator = unicodeList.GetEnumerator();

                // write out unicode list
            using (StreamWriter streamWriter = new StreamWriter(unicodeFile))
            {
                while (enumerator.MoveNext())
                {
                    streamWriter.WriteLine("\\u" + String.Format("{0:x4}", (int)enumerator.Key) + " = '" + enumerator.Value.ToString() + "'");
                }

                streamWriter.Close();
            }
            
            Console.WriteLine("Completed Unicode Collection at " + DateTime.Now);  
        }

        static void processIhtmlTransform() 
        {
            Console.WriteLine("Started IHTML Transformation at " + DateTime.Now);  

            BackgroundWorker worker = new BackgroundWorker();
            string htmlString = outputTarPath + "ihtml/index.html";
            
            string xsltString = bookSrcPath + "xslt/" + BookXsltIthml;
            XPathDocument mxml = new XPathDocument(mxmlFile);
            int numberOfPages = mxml.CreateNavigator().Select("//*[(name() = 'table' and @cols > 2) or (name() = 'img' and (@width > 280 or @height > 450)) or (name() = 'index-item') or (name() = 'container' and ((@pres-type = 'hard' or @pres-type = 'hidden') and not(parent::index-item and child::table[@cols > 2])))]").Count;
            MultiHtmlTextWriter htmlMultiWriter = new MultiHtmlTextWriter(htmlString, System.Text.Encoding.GetEncoding("utf-8"), numberOfPages);
            MvpXslTransform htmlTransform = new MvpXslTransform ();
            XmlUrlResolver resolver = new XmlUrlResolver();
            XsltSettings xsltSettings = new XsltSettings (true, true);
            XmlInput input = new XmlInput(mxmlFile);

            htmlTransform.Load(xsltString, xsltSettings, resolver);
            htmlTransform.Transform(input, null, new XmlOutput(htmlMultiWriter));

            Console.WriteLine("Completed IHTML Transformation at " + DateTime.Now);  
        }

        static void InitializeFromXML(string DefineXmlName) 
        {
            XmlNode resource = null;
            string dPath = null;
            XmlDocument dXmlDoc = null;
            try
            {   
                dPath = Path.GetFullPath(DefineXmlName);
                dXmlDoc = new XmlDocument();
                dXmlDoc.Load(dPath);
                resource = dXmlDoc.SelectSingleNode("//resource");
                BookDir = resource.SelectSingleNode("//book").Attributes["dir"].InnerText;
                BookName = resource.SelectSingleNode("//book").Attributes["name"].InnerText;
                BookSrc = resource.SelectSingleNode("//book").Attributes["src"].InnerText;
                BookXml = resource.SelectSingleNode("//xml").Attributes["src"].InnerText;
                BookXslt = resource.SelectSingleNode("//xslt").Attributes["src"].InnerText;
                BookMxml = resource.SelectSingleNode("//mxml").Attributes["src"].InnerText;
                BookUnicode = resource.SelectSingleNode("//unicode").Attributes["src"].InnerText;   
                BookXsltIthml = resource.SelectSingleNode("//ihtml").Attributes["src"].InnerText;

                mxmlFile = outputTarPath + "mxml/" + BookMxml;
                unicodeFile =  outputTarPath + "unicode/" + BookUnicode;             
            }
            catch (Exception xpe)
            { 
                
            }
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
