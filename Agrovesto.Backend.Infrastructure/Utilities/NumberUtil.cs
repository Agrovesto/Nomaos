using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class NumberUtil
    {

        private static readonly Random random = new Random();

        public static string GetLast(this string source, int tailLength)
        {
            if (tailLength >= source.Length)
                return source;
            return source.Substring(source.Length - tailLength);
        }
    }

    public static class PhoneNumberValidationUtil
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();
        private static readonly string[] AllowedPrefixes = { "070", "080", "081", "090", "091" };

        public static string Validate(string phoneNumber)
        {
            //if (string.IsNullOrEmpty(phoneNumber))
            //{
            //    throw new BaseException(Enums.ExceptionType.BAD_REQUEST, "Phone number is required.");
            //}

            //if (phoneNumber.Length != 11)
            //{
            //    throw new BaseException(Enums.ExceptionType.BAD_REQUEST, "Phone number must be 11 characters long");
            //}

            //if (!AllowedPrefixes.Any(prefix => phoneNumber.StartsWith(prefix)))
            //{
            //    throw new BaseException(Enums.ExceptionType.BAD_REQUEST, "Phone number must start with 070, 080, 081, 090, or 091.");
            //}

            //phoneNumber = $"234{phoneNumber.Substring(1)}";

            ValidatePhone(phoneNumber);

            return phoneNumber;
        }

        private static void ValidatePhone(string phone)
        {
            try
            {
                var parsedNumber = PhoneUtil.Parse(phone, "NG");
                if (PhoneUtil.IsValidNumber(parsedNumber))
                {
                    return;
                }
            }
            catch (NumberParseException)
            {
               // throw new BaseException(Enums.ExceptionType.OPERATION_FAILED, "Phone number is invalid.");
            }

            //throw new BaseException(Enums.ExceptionType.OPERATION_FAILED, "Phone number is invalid.");
        }
    }


}
