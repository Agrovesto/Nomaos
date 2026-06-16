using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Exceptions
{
    public class DuplicatePhoneException : Exception
    {
        public string PhoneNumber { get; }
        public int RowIndex { get; }

        public DuplicatePhoneException(string phoneNumber, int rowIndex)
            : base($"Row {rowIndex}: A farmer with phone number '{phoneNumber}' already exists.")
        {
            PhoneNumber = phoneNumber;
            RowIndex = rowIndex;
        }
    }
}
