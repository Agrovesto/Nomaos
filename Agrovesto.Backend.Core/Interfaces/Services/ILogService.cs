using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface ILogService
    {
        /// Information
        void Information(string messageTemplate);
        void Information<T>(string messageTemplate, T propertyValue);
        void Information(string messageTemplate, params object[] propertyValues);

        void Information(Exception exception, string messageTemplate);
        void Information<T>(Exception exception, string messageTemplate, T propertyValue);
        void Information(Exception exception, string messageTemplate, params object[] propertyValues);

        /// Debug
        void Debug(string messageTemplate);
        void Debug<T>(string messageTemplate, T propertyValue);
        void Debug(string messageTemplate, params object[] propertyValues);

        void Debug(Exception exception, string messageTemplate);
        void Debug<T>(Exception exception, string messageTemplate, T propertyValue);
        void Debug(Exception exception, string messageTemplate, params object[] propertyValues);

        /// Error
        void Error(string messageTemplate);
        void Error<T>(string messageTemplate, T propertyValue);
        void Error(string messageTemplate, params object[] propertyValues);

        void Error(Exception exception, string messageTemplate);
        void Error<T>(Exception exception, string messageTemplate, T propertyValue);
        void Error(Exception exception, string messageTemplate, params object[] propertyValues);


        /// Warning
        void Warning(string messageTemplate);
        void Warning<T>(string messageTemplate, T propertyValue);
        void Warning(string messageTemplate, params object[] propertyValues);

        void Warning(Exception exception, string messageTemplate);
        void Warning<T>(Exception exception, string messageTemplate, T propertyValue);
        void Warning(Exception exception, string messageTemplate, params object[] propertyValues);
    }

}
