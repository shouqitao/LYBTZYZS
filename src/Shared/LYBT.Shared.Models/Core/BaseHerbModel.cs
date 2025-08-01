using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 中药材基础模型 - 前后端共享核心字段
    /// 包含所有通用的中药材信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseHerbModel
    {
        /// <summary>药材唯一标识</summary>
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（统一命名）</summary>
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>五笔码（统一命名）</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>库存数量</summary>
        [DisplayName("库存数量")]
        public int Stock { get; set; }

        /// <summary>批号</summary>
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>有效期</summary>
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("药材状态")]
        public HerbStatus Status { get; set; } = HerbStatus.Active;

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        [System.ComponentModel.DataAnnotations.Schema.Column("CreatedAt")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 库存状态描述（计算属性）
        /// </summary>
        [DisplayName("库存状态")]
        public string StockStatusDescription => Stock <= 0 ? "缺货" : Stock < 10 ? "库存不足" : "正常";

        /// <summary>
        /// 是否过期（计算属性）
        /// </summary>
        [DisplayName("是否过期")]
        public bool IsExpired => ExpireDate.HasValue && ExpireDate.Value < DateTime.Now;

        /// <summary>
        /// 是否即将过期（计算属性）
        /// </summary>
        [DisplayName("是否即将过期")]
        public bool IsExpiringSoon => ExpireDate.HasValue && ExpireDate.Value < DateTime.Now.AddDays(30);

        /// <summary>
        /// 药材状态显示文本（计算属性）
        /// </summary>
        [DisplayName("状态")]
        public string StatusDisplayName => Status.GetDescription();
    }
}