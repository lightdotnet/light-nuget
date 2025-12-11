using System.Net;

namespace Light.Exceptions;

public class ValidationException : ExceptionBase
{
    private const string errorMsg = "One or more validation failures have occurred.";

    public ValidationException()
        : base(errorMsg, HttpStatusCode.BadRequest)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> validationErrors)
        : base(errorMsg, HttpStatusCode.BadRequest)
    {
        ValidationErrors = validationErrors;
    }

    public IDictionary<string, string[]> ValidationErrors { get; }
}
