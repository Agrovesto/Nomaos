using Agrovesto.Backend.Core.CoreUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class AdminResponseCodes
    {

        public const string Success = "SUCCESS";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string Failed = "FAILED";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string ServerError = "SERVER_ERROR";
    }

    public static class ResponseFactory
    {
        public static Response<T> Ok<T>(T? data, string message = "Request successful.") =>
            new() { Code = AdminResponseCodes.Success, Succeeded = true, Message = message, Data = data };

        public static Response<T> Fail<T>(string code, string message) =>
            new() { Code = code, Succeeded = false, Message = message };
    }

    public static class ControllerResponseExtensions
    {
        // Keeps real HTTP status codes alongside the Response<T> body.
        // If your other endpoints always return 200, replace the body with: return c.Ok(r);
        public static IActionResult ToActionResult<T>(this ControllerBase c, Response<T> r,
            int successStatus = StatusCodes.Status200OK)
        {
            if (r.Succeeded) return c.StatusCode(successStatus, r);

            var status = r.Code switch
            {
                AdminResponseCodes.ValidationError => StatusCodes.Status400BadRequest,
                AdminResponseCodes.NotFound => StatusCodes.Status404NotFound,
                AdminResponseCodes.Conflict => StatusCodes.Status409Conflict,
                AdminResponseCodes.Unauthorized => StatusCodes.Status401Unauthorized,
                AdminResponseCodes.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };
            return c.StatusCode(status, r);
        }
    }

}
