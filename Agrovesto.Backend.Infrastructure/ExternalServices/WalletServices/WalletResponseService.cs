using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices
{
    public class AccountValidationResponse
    {
        public string WalletNumber { get; set; }
        public string AccountName { get; set; } // optional
    }

    public class WalletResponseService<T>
    {
        public int StatusCode { get; set; }
        public bool IsSucess { get; set; } 
        public string Status { get; set; } // Success, Failed, Pending
        public string Message { get; set; }
        public T Data { get; set; }

        public static WalletResponseService<T> Success(T data, string message = "Successful", int code = 200)
        {
            return new WalletResponseService<T>
            {
                IsSucess = true,
                StatusCode = code,
                Status = "Success",
                Message = message,
                Data = data

            };
        }

        public static WalletResponseService<T> Pending(T data, string message = "Pending", int code = 202)
        {
            return new WalletResponseService<T>
            {
                IsSucess= true,
                StatusCode = code,
                Status = "Pending",
                Message = message,
                Data = data
            };
        }

        public static WalletResponseService<T> Failed(string message, int code = 400)
        {
            return new WalletResponseService<T>
            {
                IsSucess = false,
                StatusCode = code,
                Status = "Failed",
                Message = message,
                Data = default
            };
        }
    }

}
