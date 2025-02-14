using Agrovesto.Backend.Core.Enums;

namespace Agrovesto.Backend.Core.Exceptions
{
    [Serializable]
    public class AlreadyExistException : BaseException
    {
        public AlreadyExistException()
            : base(ExceptionType.OPERATION_FAILED)
        {
        }
        public AlreadyExistException(string message)
            : base(ExceptionType.OPERATION_FAILED, message)
        {
        }
    }
}
