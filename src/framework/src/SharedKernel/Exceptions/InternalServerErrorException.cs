using System.Net;

namespace Light.Exceptions;

public class InternalServerErrorException(string message)
    : ExceptionBase(message, HttpStatusCode.InternalServerError);