using System;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.TreatmentRoom
{
    /// <summary>
    /// 理疗项目目录DTO
    /// </summary>
    public class TreatmentCatalogDto
    {
        /// <summary>项目ID</summary>
        [DisplayName("项目ID")]
        public Guid Id { get; set; }

        /// <summary>项目编码</summary>
        [DisplayName("项目编码")]
        public string Code { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        [DisplayName("项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目分类</summary>
        [DisplayName("项目分类")]
        public string Category { get; set; } = string.Empty;

        /// <summary>项目描述</summary>
        [DisplayName("项目描述")]
        public string? Description { get; set; }

        /// <summary>价格</summary>
        [DisplayName("价格")]
        public decimal Price { get; set; }

        /// <summary>时长(分钟)</summary>
        [DisplayName("时长")]
        public int Duration { get; set; }

        /// <summary>注意事项</summary>
        [DisplayName("注意事项")]
        public string? Precautions { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }
}