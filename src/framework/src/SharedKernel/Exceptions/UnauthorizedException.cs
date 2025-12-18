using System.Net;

namespace Light.Exceptions;

public class UnauthorizedException(string message = "Unauthorized")
    : ExceptionBase(message, HttpStatusCode.Unauthorized);