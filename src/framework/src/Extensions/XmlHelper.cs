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

        /// <summary>
        /// Get the first element with the specified tag name, or <c>null</c> when none exists.
        /// </summary>
        public static XmlNode? GetFirstElementByTagName(this XmlDocument xmlDocument, string tagName) =>
            xmlDocument.GetElementsByTagName(tagName)[0];
    }
}
