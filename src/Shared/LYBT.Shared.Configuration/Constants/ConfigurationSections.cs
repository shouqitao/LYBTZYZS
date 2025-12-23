namespace LYBT.Shared.Configuration.Constants;

/// <summary>
/// 配置节名称常量
/// </summary>
public static class ConfigurationSections
{
    // 通用配置
    public const string Jwt = "Jwt";

    // 服务端配置
    public const string Database = "Database";
    public const string Swagger = "Swagger";
    public const string Json = "Json";
    public const string Security = "Security";
    public const string Session = "Session";
    public const string Logging = "Logging";
    public const string UserManagement = "UserManagement";
    public const string SystemAdmin = "SystemAdmin";
    public const string PasswordPolicy = "PasswordPolicy";
    public const string DefaultPasswords = "DefaultPasswords";
    public const string MemoryCache = "MemoryCache";

    // 客户端配置
    public const string ApiClient = "ApiClient";
    public const string ClientSession = "ClientSession";
    public const string FeatureToggles = "FeatureToggles";
    public const string ClinicSettings = "ClinicSettings";
    public const string Prescription = "Prescription";
}
