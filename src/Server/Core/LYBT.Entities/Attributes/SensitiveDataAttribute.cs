using System;

namespace LYBT.Entities.Attributes
{
    /// <summary>
    /// 敏感数据标记特性 - Epic 05-P0-03: 数据安全保障
    /// 用于标记需要加密存储的敏感数据字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SensitiveDataAttribute : Attribute
    {
        /// <summary>
        /// 敏感数据类型
        /// </summary>
        public SensitiveDataType DataType { get; set; } = SensitiveDataType.PersonalInfo;

        /// <summary>
        /// 是否需要日志脱敏
        /// </summary>
        public bool RequireLogMasking { get; set; } = true;

        /// <summary>
        /// 脱敏模式
        /// </summary>
        public MaskingMode MaskingMode { get; set; } = MaskingMode.Default;

        public SensitiveDataAttribute(SensitiveDataType dataType = SensitiveDataType.PersonalInfo)
        {
            DataType = dataType;
        }
    }

    /// <summary>
    /// 敏感数据类型
    /// </summary>
    public enum SensitiveDataType
    {
        /// <summary>个人信息</summary>
        PersonalInfo,
        /// <summary>医疗信息</summary>
        MedicalInfo,
        /// <summary>联系信息</summary>
        ContactInfo,
        /// <summary>身份信息</summary>
        IdentityInfo,
        /// <summary>财务信息</summary>
        FinancialInfo
    }

    /// <summary>
    /// 脱敏模式
    /// </summary>
    public enum MaskingMode
    {
        /// <summary>默认脱敏（中间位用*替代）</summary>
        Default,
        /// <summary>部分隐藏（显示前后几位）</summary>
        Partial,
        /// <summary>完全隐藏</summary>
        Full,
        /// <summary>哈希脱敏</summary>
        Hash
    }
}