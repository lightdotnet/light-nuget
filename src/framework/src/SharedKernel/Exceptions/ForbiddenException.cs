using System.Net;

namespace Light.Exceptions;

public class ForbiddenException(string message)
    : ExceptionBase(message, HttpStatusCode.Forbidden);