using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Herbs
{
    /// <summary>
    /// 药材信息模型 - 前端专用，继承共享基础模型
    /// UltraThink架构Layer 4: Info模型，专为WPF桌面UI设计
    /// </summary>
    public class HerbInfo : BaseHerb
    {
        #region UI状态属性
        /// <summary>是否被选中（用于列表多选操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否展开详情（用于树形或可折叠视图）</summary>
        public bool IsExpanded { get; set; }

        /// <summary>是否正在编辑模式</summary>
        public bool IsEditing { get; set; }

        /// <summary>是否正在加载中</summary>
        public bool IsLoading { get; set; }
        #endregion

        #region 显示逻辑属性
        /// <summary>状态显示文本</summary>
        public string StatusText => Status switch
        {
            CommonStatus.Enabled => "启用",
            CommonStatus.Disabled => "禁用",
            _ => "未知"
        };

        /// <summary>价格显示文本</summary>
        public string PriceDisplay => $"¥{Price:F2}/{Unit}";

        /// <summary>库存状态显示</summary>
        public string StockDisplay => Stock > 0 ? $"库存：{Stock}" : "缺货";

        /// <summary>库存状态颜色（用于UI样式绑定）</summary>
        public string StockStatusColor => Stock > 10 ? "Green" : Stock > 0 ? "Orange" : "Red";

        /// <summary>完整显示名称</summary>
        public string FullDisplayName => $"{Name} {PriceDisplay}";

        /// <summary>产地规格显示</summary>
        public string OriginSpecDisplay => string.IsNullOrEmpty(Origin) && string.IsNullOrEmpty(Spec) 
            ? "未知产地" 
            : $"{Origin ?? "未知"} {Spec ?? ""}".Trim();

        /// <summary>功效简要显示</summary>
        public string EffectBrief => string.IsNullOrEmpty(Effect) ? "功效未录入" : 
            Effect.Length > 20 ? Effect.Substring(0, 20) + "..." : Effect;
        #endregion

        #region 前端扩展字段
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
        #endregion
    }
}