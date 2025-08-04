using System;

namespace LYBT.WPF.Client.Core.Models.TreatmentRoom
{
    /// <summary>
    /// 理疗项目目录信息
    /// </summary>
    public class TreatmentCatalogInfo
    {
        /// <summary>项目ID</summary>
        public Guid Id { get; set; }

        /// <summary>项目编码</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>项目分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>项目描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>价格</summary>
        public decimal Price { get; set; }

        /// <summary>时长(分钟)</summary>
        public int Duration { get; set; }

        /// <summary>注意事项</summary>
        public string Precautions { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        public string Indications { get; set; } = string.Empty;

        /// <summary>禁忌症</summary>
        public string Contraindications { get; set; } = string.Empty;

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>显示名称（用于界面显示）</summary>
        public string DisplayName => $"{Code} - {Name}";

        /// <summary>价格显示</summary>
        public string PriceDisplay => $"¥{Price:F2}";

        /// <summary>时长显示</summary>
        public string DurationDisplay => $"{Duration}分钟";
    }
}