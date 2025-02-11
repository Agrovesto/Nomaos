using Agrovesto.Backend.Core.Enums;

namespace Agrovesto.Backend.Core.Exceptions
{
    [Serializable]
    public class NotFoundException : BaseException
    {
        public NotFoundException()
            : base(ExceptionType.NOTFOUND)
        {
        }

        public NotFoundException(string message)
            : base(ExceptionType.NOTFOUND, message)
        {
        }
    }
}
