namespace LYBT.Shared.Utilities.Configuration
{
    /// <summary>
    /// 环境管理帮助类
    /// </summary>
    public static class EnvironmentHelper
    {
        /// <summary>
        /// 环境名称常量
        /// </summary>
        public static class Environments
        {
            public const string Development = "Development";
            public const string Staging = "Staging";
            public const string Production = "Production";
        }

        /// <summary>
        /// 获取当前环境名称
        /// </summary>
        /// <param name="defaultEnvironment">默认环境名称</param>
        /// <returns>环境名称</returns>
        public static string GetCurrentEnvironment(string defaultEnvironment = Environments.Development)
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                   Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                   defaultEnvironment;
        }

        /// <summary>
        /// 检查是否为开发环境
        /// </summary>
        /// <returns>是否为开发环境</returns>
        public static bool IsDevelopment()
        {
            var environment = GetCurrentEnvironment();
            return string.Equals(environment, Environments.Development, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否为预发布环境
        /// </summary>
        /// <returns>是否为预发布环境</returns>
        public static bool IsStaging()
        {
            var environment = GetCurrentEnvironment();
            return string.Equals(environment, Environments.Staging, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否为生产环境
        /// </summary>
        /// <returns>是否为生产环境</returns>
        public static bool IsProduction()
        {
            var environment = GetCurrentEnvironment();
            return string.Equals(environment, Environments.Production, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取环境变量值
        /// </summary>
        /// <param name="key">环境变量名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>环境变量值</returns>
        public static string GetEnvironmentVariable(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }

        /// <summary>
        /// 获取必需的环境变量
        /// </summary>
        /// <param name="key">环境变量名</param>
        /// <returns>环境变量值</returns>
        /// <exception cref="InvalidOperationException">环境变量不存在时抛出异常</exception>
        public static string GetRequiredEnvironmentVariable(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"环境变量 '{key}' 未设置");
            }
            return value;
        }

        /// <summary>
        /// 设置环境变量
        /// </summary>
        /// <param name="key">环境变量名</param>
        /// <param name="value">值</param>
        /// <param name="target">目标范围</param>
        public static void SetEnvironmentVariable(
            string key,
            string value,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            Environment.SetEnvironmentVariable(key, value, target);
        }

        /// <summary>
        /// 获取环境特定的配置文件名
        /// </summary>
        /// <param name="baseFileName">基础文件名</param>
        /// <param name="environment">环境名称（可选）</param>
        /// <returns>环境特定的配置文件名</returns>
        public static string GetEnvironmentSpecificFileName(string baseFileName, string? environment = null)
        {
            var env = environment ?? GetCurrentEnvironment();
            var extension = Path.GetExtension(baseFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

            return $"{fileNameWithoutExtension}.{env}{extension}";
        }

        /// <summary>
        /// 根据环境选择值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="developmentValue">开发环境值</param>
        /// <param name="stagingValue">预发布环境值</param>
        /// <param name="productionValue">生产环境值</param>
        /// <returns>根据当前环境选择的值</returns>
        public static T SelectByEnvironment<T>(
            T developmentValue,
            T stagingValue,
            T productionValue)
        {
            if (IsDevelopment()) return developmentValue;
            if (IsStaging()) return stagingValue;
            if (IsProduction()) return productionValue;

            // 默认返回开发环境值
            return developmentValue;
        }

        /// <summary>
        /// 获取机器信息
        /// </summary>
        /// <returns>机器信息</returns>
        public static MachineInfo GetMachineInfo()
        {
            return new MachineInfo
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                UserName = Environment.UserName,
                UserDomainName = Environment.UserDomainName,
                CurrentDirectory = Environment.CurrentDirectory,
                SystemDirectory = Environment.SystemDirectory
            };
        }

        /// <summary>
        /// 验证环境配置
        /// </summary>
        /// <param name="requiredVariables">必需的环境变量列表</param>
        /// <returns>验证结果</returns>
        public static EnvironmentValidationResult ValidateEnvironment(params string[] requiredVariables)
        {
            var result = new EnvironmentValidationResult();

            foreach (var variable in requiredVariables)
            {
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
                {
                    result.MissingVariables.Add(variable);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 机器信息
    /// </summary>
    public class MachineInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserDomainName { get; set; } = string.Empty;
        public string CurrentDirectory { get; set; } = string.Empty;
        public string SystemDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 环境验证结果
    /// </summary>
    public class EnvironmentValidationResult
    {
        /// <summary>
        /// 缺失的环境变量
        /// </summary>
        public List<string> MissingVariables { get; } = new();

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid => MissingVariables.Count == 0;

        /// <summary>
        /// 获取错误消息
        /// </summary>
        /// <returns>错误消息</returns>
        public string GetErrorMessage()
        {
            if (IsValid)
                return string.Empty;

            return $"以下环境变量缺失: {string.Join(", ", MissingVariables)}";
        }
    }
}
