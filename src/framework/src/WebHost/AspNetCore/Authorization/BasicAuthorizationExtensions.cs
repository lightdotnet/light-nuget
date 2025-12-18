using Microsoft.AspNetCore.Http;
using System.Text;

namespace Light.AspNetCore.Authorization;

public static class BasicAuthorizationExtensions
{
    public static string ReadBasicAuthorization(this HttpRequest httpRequest)
    {
        // read Authorization from request header
        // Authorization value from header looks similar to this "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ=="
        var authHeader = httpRequest.Headers.Authorization.ToString();

        // remove "Basic " for get encoded Username & Password
        string encodedUsernamePassword = authHeader.Replace("Basic ", "").Trim();

        Encoding encoding = Encoding.GetEncoding("iso-8859-1");

        // encoded value after decode similar to Username:Password
        return encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
    }
}
