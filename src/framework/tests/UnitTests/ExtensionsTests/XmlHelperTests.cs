namespace UnitTests.ExtensionsTests
{
    public class XmlHelperTests
    {
        [Fact]
        public void Should_Get_Correct_Success_Data()
        {
            var xml = $@"<?xml version=""1.0"" ?><S:Envelope xmlns:S=""http://schemas.xmlsoap.org/soap/envelope/""><S:Body><ns2:wsCpMtResponse xmlns:ns2=""http://impl.bulkSms.ws/""><return><message>Insert MT_QUEUE: OK</message><result>1</result></return></ns2:wsCpMtResponse></S:Body></S:Envelope>";

            var responseMessage = "Insert MT_QUEUE: OK";
            var responseResultCode = "1";

            var doc = XmlHelper.LoadXml(xml);

            var message = doc.GetFirstElementByTagName("message")?.InnerText;
            var result = doc.GetFirstElementByTagName("result")?.InnerText;

            Assert.Equal(responseMessage, message);
            Assert.Equal(responseResultCode, result);
        }

        [Fact]
        public void Should_Get_Correct_Error_Data()
        {
            var xml = $@"<?xml version=""1.0"" ?><S:Envelope xmlns:S=""http://schemas.xmlsoap.org/soap/envelope/""><S:Body><ns2:wsCpMtResponse xmlns:ns2=""http://impl.bulkSms.ws/""><return><message>Check template: CONTENT_NOT_MATCH_TEMPLATE</message><result>0</result></return></ns2:wsCpMtResponse></S:Body></S:Envelope>";

            var responseMessage = "Check template: CONTENT_NOT_MATCH_TEMPLATE";
            var responseResultCode = "0";

            var doc = XmlHelper.LoadXml(xml);

            var message = doc.GetFirstElementByTagName("message")?.InnerText;
            var result = doc.GetFirstElementByTagName("result")?.InnerText;

            Assert.Equal(responseMessage, message);
            Assert.Equal(responseResultCode, result);
        }
    }
}
