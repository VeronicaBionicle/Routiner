using System.Text;
using System.Text.RegularExpressions;

namespace BankXmlCleaner
{
    internal class Program
    {
        internal static void RectifyXML()
        {
            //the path to the xml file
            string path = @"D:\RoutinerProject\20240830_ED807_full.xml";
            string clearPath = @"D:\RoutinerProject\20240830_ED807_clean.xml";
            //create the xmldocument
            System.Xml.XmlDocument CXML = new System.Xml.XmlDocument();
            //load the xml into the XmlDocument
            CXML.Load(path); 
            string correctedXMlString = Regex.Replace(CXML.InnerXml, @"[^\u0001-\uD7FF\uE000-\uFFFD\ud800]", string.Empty);
            CXML.LoadXml(correctedXMlString);
            CXML.Save(clearPath);
        }

        static void Main(string[] args)
        {
            RectifyXML();
        }
    }
}
