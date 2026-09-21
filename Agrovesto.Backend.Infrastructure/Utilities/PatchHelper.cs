using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class PatchHelper
    {
        /// <summary>
        /// PATCH semantics: null / blank => "not provided, leave as is". Applies only when the value differs.
        /// Records the field name so callers can report what really changed.
        /// </summary>
        public static bool Patch<T>(ICollection<string> changed, string field, T incoming, T current, Action<T> apply)
        {
            if (incoming is null) return false;
            if (incoming is string s && string.IsNullOrWhiteSpace(s)) return false;
            if (EqualityComparer<T>.Default.Equals(incoming, current)) return false;

            apply(incoming);
            changed.Add(field);
            return true;
        }
    }

    public static class DbErrors
    {
        private static readonly (string Key, string Label)[] KnownFields =
        {
        ("NIN", "NIN"), ("BVN", "BVN"), ("Email", "email"), ("Phone", "phone number")
    };

        /// <summary>
        /// Provider-agnostic unique-violation check (works for MySQL/MariaDB 1062 and SQL Server 2601/2627),
        /// so it survives the MSSQL -> MySQL migration without referencing either driver.
        /// </summary>
        public static bool IsDuplicateKey(DbUpdateException ex, out string field)
        {
            field = "value";
            var inner = ex.InnerException;
            if (inner is null) return false;

            var number = inner.GetType().GetProperty("Number")?.GetValue(inner) as int?;
            if (number is not (1062 or 2601 or 2627)) return false;

            foreach (var (key, label) in KnownFields)
            {
                if (inner.Message.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    field = label;
                    break;
                }
            }
            return true;
        }
    }
}
