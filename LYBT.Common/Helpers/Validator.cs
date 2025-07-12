using System.Text.RegularExpressions;

namespace LYBT.Common.Helpers {
    /// <summary>
    /// Common validation helpers.
    /// </summary>
    public static class Validator {
        /// <summary>
        /// Validate Chinese ID number (18 digits with checksum).
        /// </summary>
        public static bool CheckIdNumber(string? idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;
            idNumber = idNumber.Trim();
            if (!Regex.IsMatch(idNumber, @"^\d{17}[\dXx]$"))
                return false;
            int[] weight = {7,9,10,5,8,4,2,1,6,3,7,9,10,5,8,4,2};
            char[] codes = "10X98765432".ToCharArray();
            int sum = 0;
            for (int i = 0; i < 17; i++) {
                sum += (idNumber[i] - '0') * weight[i];
            }
            char code = codes[sum % 11];
            return char.ToUpperInvariant(idNumber[17]) == code;
        }
    }
}
