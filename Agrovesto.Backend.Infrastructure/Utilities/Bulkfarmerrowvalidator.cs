
using Agrovesto.Backend.Core.Models.Businesses;
using System.Text.RegularExpressions;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
   
    public static class BulkFarmerRowValidator
    {
        private static readonly Regex PhoneRegex =
            new(@"^\+?[0-9]{7,15}$", RegexOptions.Compiled);

        /// <summary>
        /// Returns a list of validation error messages for a single row.
        /// An empty list means the row is valid.
        /// </summary>
        public static List<string> Validate(BulkFarmerRowDto row)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Name))
                errors.Add("Name is required.");

            if (string.IsNullOrWhiteSpace(row.PhoneNumber))
                errors.Add("PhoneNumber is required.");
            else if (!PhoneRegex.IsMatch(row.PhoneNumber.Trim()))
                errors.Add($"PhoneNumber '{row.PhoneNumber}' is not valid.");

            if (!string.IsNullOrWhiteSpace(row.Email) &&
                !row.Email.Contains('@'))
                errors.Add($"Email '{row.Email}' is not valid.");

            if (row.YearsInAgric < 0)
                errors.Add("YearsInAgric cannot be negative.");

            return errors;
        }
    }
}
