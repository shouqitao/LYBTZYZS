namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务常量定义
    /// </summary>
    public static class PatientConstants
    {
        /// <summary>
        /// 身份证号长度
        /// </summary>
        public const int IdNumberLength = 18;

        /// <summary>
        /// 流失患者判断天数（默认180天）
        /// </summary>
        public const int InactivePatientsDefaultDays = 180;

        /// <summary>
        /// 活跃患者判断天数（默认30天）
        /// </summary>
        public const int ActivePatientsDefaultDays = 30;

        /// <summary>
        /// 默认统计月份数
        /// </summary>
        public const int DefaultStatisticsMonths = 12;

        /// <summary>
        /// 单次查询最大数量
        /// </summary>
        public const int MaxQueryLimit = 10000;

        /// <summary>
        /// 年龄分布范围
        /// </summary>
        public static readonly (int Min, int Max, string Range)[] AgeRanges = new[]
        {
            (0, 18, "0-18岁（儿童）"),
            (19, 35, "19-35岁（青年）"),
            (36, 50, "36-50岁（中年）"),
            (51, 65, "51-65岁（中老年）"),
            (66, int.MaxValue, "66岁以上（老年）")
        };
    }
}