using Agrovesto.Backend.Core.Enums;
using System.Net;

namespace Agrovesto.Backend.Core.Exceptions
{
    public class BaseException : Exception
    {
        public HttpStatusCode HttpStatusCode { get; }
        public string ExceptionMessage { get; }

        public BaseException(ExceptionType exceptionType, string? customMessage = null)
            : base(customMessage ?? ResolveMessage(exceptionType))
        {
            HttpStatusCode = ResolveHttpStatus(exceptionType);
            ExceptionMessage = customMessage ?? ResolveMessage(exceptionType);
        }

        private static HttpStatusCode ResolveHttpStatus(ExceptionType type)
        {
            return type switch
            {
                ExceptionType.BAD_REQUEST => HttpStatusCode.BadRequest,
                ExceptionType.ALREADY_EXIST => HttpStatusCode.Conflict,
                ExceptionType.NO_RECORD_FOUND => HttpStatusCode.NotFound,
                ExceptionType.UNAUTHORIZED => HttpStatusCode.Unauthorized,
                ExceptionType.OPERATION_FAILED => HttpStatusCode.ExpectationFailed,
                _ => HttpStatusCode.InternalServerError,
            };
        }

        private static string ResolveMessage(ExceptionType type)
        {
            return type switch
            {
                ExceptionType.BAD_REQUEST => "INVALID_INPUT",
                ExceptionType.ALREADY_EXIST => "ALREADY_EXIST",
                ExceptionType.NO_RECORD_FOUND => "NO_RECORD_FOUND",
                ExceptionType.UNAUTHORIZED => "UNAUTHORIZED",
                ExceptionType.OPERATION_FAILED => "FAILED",
                _ => "INTERNAL_SERVER_ERROR",
            };
        }
    }

}



