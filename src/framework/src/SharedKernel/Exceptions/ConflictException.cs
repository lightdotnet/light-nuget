using System.Net;

namespace Light.Exceptions;

public class ConflictException(string message)
    : ExceptionBase(message, HttpStatusCode.Conflict);