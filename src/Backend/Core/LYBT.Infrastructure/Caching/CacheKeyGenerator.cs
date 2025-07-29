using System.Text;

namespace LYBT.Infrastructure.Caching {

    /// <summary>
    /// 缓存键生成器
    /// </summary>
    public static class CacheKeyGenerator {

        /// <summary>
        /// 生成用户相关的缓存键
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GenerateUserKey(string userId, string suffix) {
            return $"user:{userId}:{suffix}";
        }

        /// <summary>
        /// 生成患者相关的缓存键
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GeneratePatientKey(string patientId, string suffix) {
            return $"patient:{patientId}:{suffix}";
        }

        /// <summary>
        /// 生成医生相关的缓存键
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GenerateDoctorKey(string doctorId, string suffix) {
            return $"doctor:{doctorId}:{suffix}";
        }

        /// <summary>
        /// 生成药材相关的缓存键
        /// </summary>
        /// <param name="herbId">药材ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GenerateHerbKey(string herbId, string suffix) {
            return $"herb:{herbId}:{suffix}";
        }

        /// <summary>
        /// 生成处方相关的缓存键
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GeneratePrescriptionKey(string prescriptionId, string suffix) {
            return $"prescription:{prescriptionId}:{suffix}";
        }

        /// <summary>
        /// 生成分页查询的缓存键
        /// </summary>
        /// <param name="module">模块名</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="additionalParams">附加参数</param>
        /// <returns>缓存键</returns>
        public static string GeneratePagedQueryKey(string module, int pageIndex, int pageSize, params object[] additionalParams) {
            var keyBuilder = new StringBuilder($"paged:{module}:{pageIndex}:{pageSize}");

            if (additionalParams?.Length > 0) {
                foreach (var param in additionalParams) {
                    keyBuilder.Append($":{param}");
                }
            }

            return keyBuilder.ToString();
        }

        /// <summary>
        /// 生成列表查询的缓存键
        /// </summary>
        /// <param name="module">模块名</param>
        /// <param name="listType">列表类型</param>
        /// <param name="additionalParams">附加参数</param>
        /// <returns>缓存键</returns>
        public static string GenerateListKey(string module, string listType, params object[] additionalParams) {
            var keyBuilder = new StringBuilder($"list:{module}:{listType}");

            if (additionalParams?.Length > 0) {
                foreach (var param in additionalParams) {
                    keyBuilder.Append($":{param}");
                }
            }

            return keyBuilder.ToString();
        }

        /// <summary>
        /// 生成统计数据的缓存键
        /// </summary>
        /// <param name="module">模块名</param>
        /// <param name="statType">统计类型</param>
        /// <param name="timeRange">时间范围</param>
        /// <returns>缓存键</returns>
        public static string GenerateStatsKey(string module, string statType, string timeRange) {
            return $"stats:{module}:{statType}:{timeRange}";
        }

        /// <summary>
        /// 生成配置相关的缓存键
        /// </summary>
        /// <param name="configType">配置类型</param>
        /// <param name="configKey">配置键</param>
        /// <returns>缓存键</returns>
        public static string GenerateConfigKey(string configType, string configKey) {
            return $"config:{configType}:{configKey}";
        }

        /// <summary>
        /// 生成会话相关的缓存键
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="suffix">后缀</param>
        /// <returns>缓存键</returns>
        public static string GenerateSessionKey(string sessionId, string suffix) {
            return $"session:{sessionId}:{suffix}";
        }

        /// <summary>
        /// 生成临时数据的缓存键
        /// </summary>
        /// <param name="category">类别</param>
        /// <param name="identifier">标识符</param>
        /// <returns>缓存键</returns>
        public static string GenerateTempKey(string category, string identifier) {
            return $"temp:{category}:{identifier}";
        }

        /// <summary>
        /// 生成锁定相关的缓存键
        /// </summary>
        /// <param name="resource">资源标识</param>
        /// <param name="operation">操作类型</param>
        /// <returns>缓存键</returns>
        public static string GenerateLockKey(string resource, string operation) {
            return $"lock:{resource}:{operation}";
        }

        /// <summary>
        /// 清理缓存键（移除特殊字符）
        /// </summary>
        /// <param name="key">原始键</param>
        /// <returns>清理后的键</returns>
        public static string SanitizeKey(string key) {
            if (string.IsNullOrEmpty(key)) {
                return key;
            }

            // 移除或替换不安全的字符
            return key.Replace(" ", "_")
                     .Replace("\t", "_")
                     .Replace("\r", "")
                     .Replace("\n", "");
        }

        /// <summary>
        /// 生成带时间戳的缓存键
        /// </summary>
        /// <param name="baseKey">基础键</param>
        /// <param name="timespan">时间粒度（如小时、天等）</param>
        /// <returns>带时间戳的缓存键</returns>
        public static string GenerateTimeBasedKey(string baseKey, TimeSpan timespan) {
            var now = DateTime.UtcNow;
            var timestamp = timespan.TotalDays >= 1 ? now.ToString("yyyyMMdd") :
                           timespan.TotalHours >= 1 ? now.ToString("yyyyMMddHH") :
                           now.ToString("yyyyMMddHHmm");

            return $"{baseKey}:{timestamp}";
        }
    }
}