using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.CoreUtils
{
    public class ResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }

        public ResponseDto(bool success, string message, object? data = null)
        {
            Status = success;
            Message = message;
            Data = data;
        }

        /// <summary>
        /// Returns a successful response with an optional data payload.
        /// </summary>
        public static ResponseDto Success(string message, object? data = null)
        {
            return new ResponseDto(true, message, data);
        }

        /// <summary>
        /// Returns a failed response with an optional data payload.
        /// </summary>
        public static ResponseDto Failure(string message, object? data = null)
        {
            return new ResponseDto(false, message, data);
        }
    }
    public class Response<T>
    {
        public string Code { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class PagedListResponse<T>
    {
        public string Code { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int Count { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Count / PageSize);

    }
    public class AgrovestoResponse<T> where T : class//: Response
    {
        /// <summary>
        /// Indicates if the response contains a result
        /// </summary>
        public bool HasResult
        {
            get
            {
                return Result != null;
            }
        }

        /// <summary>
        /// The result of the response
        /// </summary>
        public T Result { get; set; }
        public ResultType ResultType { get; set; }
        public string Message { get; set; }
        public List<string> ValidationMessages { get; set; }


        /// <summary>
        /// Indicates if the response is successful or not. Warning or success result type indicate success
        /// </summary>
        public bool Successful
        {
            get
            {
                return ResultType == ResultType.Success || ResultType == ResultType.Warning;
            }
        }
        /// <summary>
        /// Creates a successful response with a given result object
        /// </summary>
        /// <param name="result">The result object to return with the response</param>
        /// <returns>The response object</returns>
        public static AgrovestoResponse<T> Success(T result)
        {
            var response = new AgrovestoResponse<T> { ResultType = ResultType.Success, Result = result };

            return response;
        }

        /// <summary>
        /// Creates a failed result. It takes no result object
        /// </summary>
        /// <param name="errorMessage">The error message returned with the response</param>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse<T> Failed(string errorMessage)
        {
            var response = new AgrovestoResponse<T> { ResultType = ResultType.Error, Message = errorMessage };

            return response;
        }



        /// <summary>
        /// Creates a validation error response, indicating the input was invalid
        /// </summary>
        /// <param name="validationMessages">The validation message</param>
        /// <returns>The Response object</returns>
        public static AgrovestoResponse<T> ValidationError(List<string> validationMessages)
        {
            var response = new AgrovestoResponse<T> { ResultType = ResultType.ValidationError, Message = "Response has validation errors", ValidationMessages = validationMessages };

            return response;
        }

        /// <summary>
        /// Creates a warning result. The warning result is successful, but might have issues that should be addressed or logged
        /// </summary>
        /// <param name="warningMessage">The warning returned with the response</param>
        /// <param name="result">The result object</param>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse<T> Warning(string warningMessage, T result)
        {
            var response = new AgrovestoResponse<T>
            {
                ResultType = ResultType.Warning,
                Message = warningMessage,
                Result = result
            };

            return response;
        }

        /// <summary>
        /// Creates an empty result. The empty result is successful, but might have issues that should be addressed or logged
        /// </summary>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse<T> Empty()
        {
            var response = new AgrovestoResponse<T> { ResultType = ResultType.Empty };

            return response;
        }

    }

    /// <summary>
    /// A simple response object with no returned object. Just indicates successful or failed requests
    /// </summary>
    public class AgrovestoResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgrovestoResponse"/> class. 
        /// </summary>
        public AgrovestoResponse()
        {
            ResultType = ResultType.Success;
        }

        /// <summary>
        /// Indicates if the response contains a result
        /// </summary>
        public virtual bool HasResult
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// The result of the response
        /// </summary>
        public object Result { get; protected set; }

        /// <summary>
        /// Indicates if the response is successful or not. Warning or success result type indicate success
        /// </summary>
        public bool Successful
        {
            get
            {
                return ResultType == ResultType.Success || ResultType == ResultType.Warning;
            }
        }


        /// <summary>
        /// The result type
        /// </summary>
        public ResultType ResultType { get; set; }

        /// <summary>
        /// The message returned with the response
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The validation error messages returned with the response
        /// </summary>
        public List<string> ValidationMessages { get; set; }

        /// <summary>
        /// Creates a successful response with a given result object
        /// </summary>
        /// <returns>The response object</returns>
        public static AgrovestoResponse Success()
        {
            var response = new AgrovestoResponse { ResultType = ResultType.Success };

            return response;
        }

        /// <summary>
        /// Creates a failed result. It takes no result object
        /// </summary>
        /// <param name="errorMessage">The error message returned with the response</param>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse Failed(string errorMessage)
        {
            var response = new AgrovestoResponse { ResultType = ResultType.Error, Message = errorMessage };

            return response;
        }

        /// <summary>
        /// Creates a validation error response, indicating the input was invalid
        /// </summary>
        /// <param name="validationMessages">The validation message</param>
        /// <returns>The Response object</returns>
        public static AgrovestoResponse ValidationError(List<string> validationMessages)
        {
            var response = new AgrovestoResponse { ResultType = ResultType.ValidationError, ValidationMessages = validationMessages };

            return response;
        }

        /// <summary>
        /// Creates a warning result. The warning result is successful, but might have issues that should be addressed or logged
        /// </summary>
        /// <param name="warningMessage">The warning returned with the response</param>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse Warning(string warningMessage)
        {
            var response = new AgrovestoResponse { ResultType = ResultType.Warning, Message = warningMessage };

            return response;
        }

        /// <summary>
        /// Creates a validation error response, indicating the input was invalid
        /// </summary>
        /// <param name="customerInformationMessage">The validation message</param>
        /// <returns>The Response object</returns>
        public static AgrovestoResponse CustomerInformation(string customerInformationMessage)
        {
            var response = new AgrovestoResponse
            {
                ResultType = ResultType.CustomerInformation,
                Message = customerInformationMessage
            };

            return response;
        }

        /// <summary>
        /// Creates an empty result. The empty result is successful, but might have issues that should be addressed or logged
        /// </summary>
        /// <returns>The created response object</returns>
        public static AgrovestoResponse Empty()
        {
            var response = new AgrovestoResponse { ResultType = ResultType.Empty };

            return response;
        }
    }

    public enum ResultType
    {
        Success = 1,
        Error = 2,
        ValidationError = 3,
        Warning = 4,
        CustomerInformation = 5,
        Empty = 6
    }
}
