using System;
using LYBT.Shared.Models.Core;

namespace LYBT.WPF.Client.Core.Models.Herbs
{
    /// <summary>
    /// 药材信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class HerbInfo : BaseHerbModel
    {
        /// <summary>总价（前端计算字段）</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>状态描述（前端显示字段）</summary>
        public string? StatusDescription { get; set; }

        /// <summary>供应商信息（前端扩展字段）</summary>
        public string? Supplier { get; set; }

        /// <summary>最后操作时间（前端业务字段）</summary>
        public DateTime? LastOperationTime { get; set; }

        /// <summary>操作人员（前端审计字段）</summary>
        public string? OperatorName { get; set; }
        
        /// <summary>分类</summary>
        public string? Category { get; set; }
        
        /// <summary>库存数量</summary>
        public decimal Stock { get; set; }
        
        /// <summary>是否激活</summary>
        public bool IsActive { get; set; } = true;
    }
}