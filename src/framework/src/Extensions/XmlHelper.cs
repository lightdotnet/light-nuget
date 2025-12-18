using System.Xml;

namespace Light.Extensions
{
    public static class XmlHelper
    {
        public static XmlDocument LoadXml(string xml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            return xmlDoc;
        }

        public static XmlNode GetFirstElementByTagName(this XmlDocument xmlDocument, string tagName) =>
            xmlDocument.GetElementsByTagName(tagName)[0];
    }
}
