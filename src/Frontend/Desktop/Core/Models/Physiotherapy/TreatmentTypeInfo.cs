using System;

namespace LYBT.WPF.Client.Core.Models.Physiotherapy
{
    /// <summary>
    /// 理疗类型信息
    /// </summary>
    public class TreatmentTypeInfo
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 类型编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 类型名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 类型描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 单次时长（分钟）
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序号
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}