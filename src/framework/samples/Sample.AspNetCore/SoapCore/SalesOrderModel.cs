using System.Xml.Serialization;

namespace Sample.AspNetCore.SoapCore;

[XmlRoot(ElementName = "body")]
public class SalesOrderModel
{
    [XmlElement(ElementName = "No")]
    public string? No { get; set; }
}
