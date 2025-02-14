using Agrovesto.Backend.Core.Enums;

namespace Agrovesto.Backend.Core.Exceptions
{
    public class ModelNullException : BaseException
    {
        public ModelNullException()
            : base(ExceptionType.NOTFOUND) 
        {
        }

        public ModelNullException(string message)
            : base(ExceptionType.NOTFOUND) 
        {
        }
    }
}

