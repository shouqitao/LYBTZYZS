namespace LYBT.Tests.Backend.TestUtilities
{
    /// <summary>
    /// 测试常量定义 - 统一的测试配置和常量值
    /// </summary>
    public static class TestConstants
    {
        #region 测试用户常量

        /// <summary>
        /// 测试管理员用户ID
        /// </summary>
        public static readonly Guid TestAdminUserId = new("11111111-1111-1111-1111-111111111111");

        /// <summary>
        /// 测试医生用户ID
        /// </summary>
        public static readonly Guid TestDoctorUserId = new("22222222-2222-2222-2222-222222222222");

        /// <summary>
        /// 测试患者ID
        /// </summary>
        public static readonly Guid TestPatientId = new("33333333-3333-3333-3333-333333333333");

        /// <summary>
        /// 默认测试密码
        /// </summary>
        public const string DefaultTestPassword = "Test123!";

        /// <summary>
        /// 测试用户名前缀
        /// </summary>
        public const string TestUsernamePrefix = "test_";

        #endregion

        #region 分页常量

        /// <summary>
        /// 默认页码
        /// </summary>
        public const int DefaultPage = 1;

        /// <summary>
        /// 默认页大小
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// 最大页大小
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>
        /// 最小页大小
        /// </summary>
        public const int MinPageSize = 1;

        #endregion

        #region 业务常量

        /// <summary>
        /// 测试诊所名称
        /// </summary>
        public const string TestClinicName = "测试中医诊所";

        /// <summary>
        /// 测试中药材名称
        /// </summary>
        public static readonly string[] TestHerbNames = 
        {
            "人参", "当归", "黄芪", "白术", "茯苓", "甘草", "川芎", "白芍"
        };

        /// <summary>
        /// 测试处方模板名称
        /// </summary>
        public static readonly string[] TestFormulaNames = 
        {
            "四君子汤", "四物汤", "逍遥散", "补中益气汤", "六味地黄丸"
        };

        #endregion

        #region 时间常量

        /// <summary>
        /// 测试基准时间
        /// </summary>
        public static readonly DateTime TestBaseDateTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 操作超时时间（毫秒）
        /// </summary>
        public const int OperationTimeoutMs = 5000;

        /// <summary>
        /// 快速操作预期时间（毫秒）
        /// </summary>
        public const int FastOperationExpectedMs = 100;

        /// <summary>
        /// 数据库操作预期时间（毫秒）
        /// </summary>
        public const int DatabaseOperationExpectedMs = 1000;

        #endregion

        #region 验证常量

        /// <summary>
        /// 有效手机号码示例
        /// </summary>
        public static readonly string[] ValidPhoneNumbers = 
        {
            "13800138000", "15912345678", "18688888888"
        };

        /// <summary>
        /// 无效手机号码示例
        /// </summary>
        public static readonly string[] InvalidPhoneNumbers = 
        {
            "12345", "abcdefghijk", "1388888888888"
        };

        /// <summary>
        /// 有效身份证号码示例
        /// </summary>
        public static readonly string[] ValidIdCardNumbers = 
        {
            "110101199001011234", "320101199912312345", "440101199808080012"
        };

        /// <summary>
        /// 无效身份证号码示例
        /// </summary>
        public static readonly string[] InvalidIdCardNumbers = 
        {
            "12345", "abcdefghijklmnopqr", "11010119900101123"
        };

        #endregion

        #region 错误消息常量

        /// <summary>
        /// 通用错误消息
        /// </summary>
        public static class ErrorMessages
        {
            public const string EntityNotFound = "未找到指定的记录";
            public const string InvalidParameter = "参数无效";
            public const string DuplicateEntry = "记录已存在";
            public const string OperationFailed = "操作失败";
            public const string AccessDenied = "访问被拒绝";
            public const string InvalidCredentials = "用户名或密码错误";
            public const string AccountLocked = "账户已被锁定";
        }

        #endregion

        #region 测试数据数量

        /// <summary>
        /// 小数据集大小
        /// </summary>
        public const int SmallDataSetSize = 5;

        /// <summary>
        /// 中等数据集大小
        /// </summary>
        public const int MediumDataSetSize = 50;

        /// <summary>
        /// 大数据集大小
        /// </summary>
        public const int LargeDataSetSize = 500;

        /// <summary>
        /// 性能测试数据集大小
        /// </summary>
        public const int PerformanceTestDataSetSize = 1000;

        #endregion

        #region 缓存常量

        /// <summary>
        /// 测试缓存键前缀
        /// </summary>
        public const string TestCacheKeyPrefix = "test_cache_";

        /// <summary>
        /// 短期缓存时间（秒）
        /// </summary>
        public const int ShortCacheDurationSeconds = 60;

        /// <summary>
        /// 长期缓存时间（秒）
        /// </summary>
        public const int LongCacheDurationSeconds = 3600;

        #endregion

        #region 测试环境配置

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public const bool EnableVerboseLogging = false;

        /// <summary>
        /// 是否启用性能测试
        /// </summary>
        public const bool EnablePerformanceTests = true;

        /// <summary>
        /// 是否启用集成测试
        /// </summary>
        public const bool EnableIntegrationTests = true;

        #endregion
    }
}