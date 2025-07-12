using System.Text.RegularExpressions;

namespace LYBT.Common.Helpers {
    /// <summary>
    /// Common string formatting utilities.
    /// </summary>
    public static class StringHelper {
        /// <summary>
        /// Format phone number as 3-4-4 pattern if length is 11, otherwise return original.
        /// </summary>
        public static string FormatPhone(string? phone) {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;
            var digits = Regex.Replace(phone, "\\n|\\r|\\s|\\D", string.Empty);
            if (digits.Length == 11)
                return $"{digits[..3]}-{digits[3..7]}-{digits[7..]}";
            if (digits.Length == 10)
                return $"{digits[..3]}-{digits[3..6]}-{digits[6..]}";
            return digits;
        }
    }
}
