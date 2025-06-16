namespace LYBT.Common.Extensions {

    public static class StringExtensions {

        public static bool IsNullOrEmpty(this string? value) {
            return string.IsNullOrEmpty(value);
        }

        public static string SafeTrim(this string? value) {
            return value?.Trim() ?? string.Empty;
        }
    }
}