namespace LYBT.Infrastructure.Auth {

    /// <summary>
    /// JWT 配置项
    /// </summary>
    public class JwtOptions {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; } = 60;
    }
}