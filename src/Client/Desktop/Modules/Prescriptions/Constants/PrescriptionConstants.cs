using LYBT.Shared.Models.Contracts.Common;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.Prescriptions.Constants
{
    /// <summary>
    /// 处方相关常量定义
    /// </summary>
    public static class PrescriptionConstants
    {
        #region 用法用量常量

        /// <summary>
        /// 常用剂数选项
        /// </summary>
        public static readonly ReadOnlyCollection<int> CommonDosageCounts = new ReadOnlyCollection<int>(
            new int[] { 3, 5, 7, 10, 14, 21, 30 });

        /// <summary>
        /// 常用用法模板
        /// </summary>
        public static readonly ReadOnlyCollection<string> CommonUsages = new ReadOnlyCollection<string>(
            new string[]
            {
                "每日1剂，水煎服，分早晚两次温服",
                "每日1剂，水煎服，分三次温服",
                "每日2剂，水煎服，分四次温服",
                "每日1剂，水煎服，早晚饭后温服",
                "每日1剂，水煎服，睡前温服",
                "每日1剂，开水泡服，代茶饮",
                "研末冲服，每次3g，每日3次",
                "每日1剂，水煎服，分2次温服，饭前服"
            });

        #endregion

        #region 输入提示常量

        /// <summary>
        /// 用法用量输入提示
        /// </summary>
        public const string UsageHint = "请输入用法用量，如：每日1剂，水煎服...";

        /// <summary>
        /// 医嘱输入提示
        /// </summary>
        public const string MedicalAdviceHint = "（可选）输入医嘱，如：忌生冷、注意休息等...";

        /// <summary>
        /// 备注输入提示
        /// </summary>
        public const string RemarkHint = "（可选）补充说明...";

        #endregion

        #region 默认值常量

        /// <summary>
        /// 默认剂数
        /// </summary>
        public const int DefaultDosageCount = 7;

        /// <summary>
        /// 默认用法
        /// </summary>
        public const string DefaultUsage = "每日1剂，水煎服，分早晚两次温服";

        /// <summary>
        /// 默认折扣（无折扣）
        /// </summary>
        public const decimal DefaultDiscount = 1.0m;

        /// <summary>
        /// 处方编号前缀
        /// </summary>
        public const string PrescriptionNumberPrefix = "RX";

        #endregion

        #region 验证常量

        /// <summary>
        /// 最大剂数限制
        /// </summary>
        public const int MaxDosageCount = 90;

        /// <summary>
        /// 最小剂数限制
        /// </summary>
        public const int MinDosageCount = 1;

        /// <summary>
        /// 最大折扣率
        /// </summary>
        public const decimal MaxDiscount = 1.0m;

        /// <summary>
        /// 最小折扣率
        /// </summary>
        public const decimal MinDiscount = 0.1m;

        /// <summary>
        /// 最大处方项目数量
        /// </summary>
        public const int MaxPrescriptionItems = 30;

        #endregion

        #region 格式化常量

        /// <summary>
        /// 处方编号格式
        /// </summary>
        public const string PrescriptionNumberFormat = "RX{0:yyyyMMdd}{1:D3}";

        /// <summary>
        /// 价格显示格式
        /// </summary>
        public const string PriceFormat = "F2";

        /// <summary>
        /// 剂量显示格式
        /// </summary>
        public const string DosageFormat = "F1";

        #endregion
    }
}