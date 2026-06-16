using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Exceptions
{
    public class DuplicateMembershipException : Exception
    {
        public int RowNumber { get; }

        public DuplicateMembershipException(string field, string value, int rowNumber)
            : base($"Row {rowNumber}: A record with {field} '{value}' already exists.")
        {
            RowNumber = rowNumber;
        }
    }
}
