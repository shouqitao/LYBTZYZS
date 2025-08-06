using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 中药材基础模型（简化版）
    /// 只保留基础信息，不包含库存管理
    /// </summary>
    public class BaseHerbModel {

        /// <summary>药材唯一标识</summary>
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位（如：克、两、钱）</summary>
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价（元/单位）</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法用量</summary>
        [DisplayName("用法用量")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }
}