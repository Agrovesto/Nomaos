using Agrovesto.Backend.Core.Enums;

namespace Agrovesto.Backend.Core.Exceptions
{
    [Serializable]
    public class NotSuccessfulException : BaseException
    {
        public NotSuccessfulException()
            : base(ExceptionType.OPERATION_FAILED)
        {
        }

        public NotSuccessfulException(string message)
            : base(ExceptionType.OPERATION_FAILED, message)
        {
        }
    }
}
