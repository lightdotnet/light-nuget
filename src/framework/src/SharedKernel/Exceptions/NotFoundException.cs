using System.Net;

namespace Light.Exceptions;

public class NotFoundException : ExceptionBase
{
    public NotFoundException(string message) : base(message, HttpStatusCode.NotFound)
    { }

    public NotFoundException(string key, string objectName) : base($"Queried object {objectName} by {key} was not found", HttpStatusCode.NotFound)
    { }
}