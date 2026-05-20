using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateException ex)
            {
                await HandleDbException(context, ex);
            }
            catch (Exception ex)
            {
                await HandleGenericException(context, ex);
            }
        }

        private async Task HandleGenericException(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
                throw ex;

            var traceId = context.TraceIdentifier;

            _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                succeeded = false,
                message = "An unexpected error occurred. Please contact support if the issue persists.",
                traceId
            });
        }

        private async Task HandleDbException(HttpContext context, DbUpdateException ex)
        {
            if (context.Response.HasStarted)
                throw ex;

            var traceId = context.TraceIdentifier;
            var sqlEx = ex.InnerException as SqlException;

            _logger.LogError(ex, "Database exception. TraceId: {TraceId}", traceId);

            var message = sqlEx != null
                ? GetDbMessage(sqlEx)
                : "A database error occurred while saving your data.";

            var statusCode = sqlEx != null
                ? GetStatusCode(sqlEx)
                : StatusCodes.Status500InternalServerError;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                succeeded = false,
                message,
                traceId
            });
        }

        private static string GetDbMessage(SqlException ex)
        {
            return ex.Number switch
            {
                515 => "A required field is missing. Please complete all required fields.",
                547 => "Invalid reference data provided.",
                2601 => "Duplicate record exists.",
                2627 => "Duplicate value is not allowed.",
                _ => "A database error occurred while saving your data."
            };
        }

        private static int GetStatusCode(SqlException ex)
        {
            return ex.Number switch
            {
                515 => StatusCodes.Status400BadRequest,
                547 => StatusCodes.Status409Conflict,
                2601 => StatusCodes.Status409Conflict,
                2627 => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
