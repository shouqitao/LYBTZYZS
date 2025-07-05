namespace LYBT.UI.WPF.Apis {
    public static class TokenProvider {
        private static string _token = string.Empty;
        public static string Token {
            get => _token;
            set => _token = value ?? string.Empty;
        }
    }
}
