namespace LYBT.Infrastructure.Auth {

    /// <summary>
    /// JWT 配置项
    /// </summary>
    public class JwtOptions {
/// <summary>
/// Secret 属性。
/// </summary>
        public string Secret { get; set; } = string.Empty;
/// <summary>
/// Issuer 属性。
/// </summary>
        public string Issuer { get; set; } = string.Empty;
/// <summary>
/// Audience 属性。
/// </summary>
        public string Audience { get; set; } = string.Empty;
/// <summary>
/// ExpireMinutes 属性。
/// </summary>
        public int ExpireMinutes { get; set; } = 60;
    }
}
