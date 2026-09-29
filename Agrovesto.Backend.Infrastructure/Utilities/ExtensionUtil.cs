using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    internal class ExtensionUtil
    {
    }
    public static class EnumExtensions
    {
        //public static string GetDisplayName(this Enum enumValue)
        //{
        //    var member = enumValue.GetType()
        //        .GetMember(enumValue.ToString())
        //        .FirstOrDefault();

        //    var attribute = member?
        //        .GetCustomAttribute<DisplayAttribute>();

        //    return attribute?.Name ?? enumValue.ToString();
        //}

        public static string GetDisplayName(Enum value)
        {
            if (value == null)
                return null;
            
            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var attribute = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;

            return attribute?.Name ?? value.ToString();
        }

        public static bool NamesMatch(string? a, string? b)
        {
            static string Normalize(string? s) =>
                string.Join(" ", (s ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}

