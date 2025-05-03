using Agrovesto.Backend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Core.Exceptions
{

    [Serializable]
    public class ValidationException : BaseException
    {
        public ValidationException()
           : base(ExceptionType.OPERATION_FAILED)
        {
        }
        public ValidationException(string message)
            : base(ExceptionType.BAD_REQUEST, message)
        {
        }
      
    }
}
