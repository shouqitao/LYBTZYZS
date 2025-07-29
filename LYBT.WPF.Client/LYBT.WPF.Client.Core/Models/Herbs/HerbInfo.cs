using System;
using LYBT.WPF.Client.Core.Enums;

namespace LYBT.WPF.Client.Core.Models.Herbs
{
    /// <summary>
    /// 药材信息模型
    /// </summary>
    public class HerbInfo
    {
        /// <summary>药材ID</summary>
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string? Pinyin { get; set; }

        /// <summary>五笔码</summary>
        public string? WuBi { get; set; }

        /// <summary>产地</summary>
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        public decimal Price { get; set; }

        /// <summary>总价</summary>
        public int TotalPrice { get; set; }

        /// <summary>库存数量</summary>
        public int Stock { get; set; }

        /// <summary>批号</summary>
        public string? BatchNo { get; set; }

        /// <summary>有效期</summary>
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        public string? Effect { get; set; }

        /// <summary>状态描述</summary>
        public string? StatusDescription { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>库存状态</summary>
        public string StockStatus => Stock <= 0 ? "缺货" : Stock < 10 ? "库存不足" : "正常";

        /// <summary>是否过期</summary>
        public bool IsExpired => ExpireDate.HasValue && ExpireDate.Value < DateTime.Now;
    }
}