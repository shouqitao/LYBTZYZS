using System;
using LYBT.Shared.Models.Core;

namespace LYBT.Desktop.Core.Models.Herbs
{
    /// <summary>
    /// 药材信息清洁模型 - UltraThink架构Layer 4清洁版本
    /// 移除所有UI属性和显示逻辑，纯数据模型
    /// 专为新的三层架构设计，与ViewModel组件分离
    /// </summary>
    public class HerbInfoClean : BaseHerb
    {
        #region 前端扩展数据字段

        /// <summary>分类</summary>
        public string? Category { get; set; }
        
        /// <summary>库存数量</summary>
        public decimal Stock { get; set; }
        
        /// <summary>供应商信息</summary>
        public string? Supplier { get; set; }

        /// <summary>最后操作时间</summary>
        public DateTime? LastOperationTime { get; set; }

        /// <summary>操作人员</summary>
        public string? OperatorName { get; set; }

        /// <summary>状态描述</summary>
        public string? StatusDescription { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        #endregion

        #region Constructor

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public HerbInfoClean()
        {
        }

        /// <summary>
        /// 从BaseHerb创建
        /// </summary>
        public HerbInfoClean(BaseHerb baseHerb)
        {
            if (baseHerb == null)
                throw new ArgumentNullException(nameof(baseHerb));

            Id = baseHerb.Id;
            Name = baseHerb.Name;
            PinYinCode = baseHerb.PinYinCode;
            Origin = baseHerb.Origin;
            Spec = baseHerb.Spec;
            Unit = baseHerb.Unit;
            Price = baseHerb.Price;
            Effect = baseHerb.Effect;
            Usage = baseHerb.Usage;
            Remark = baseHerb.Remark;
            Status = baseHerb.Status;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建空的药材信息
        /// </summary>
        public static HerbInfoClean CreateEmpty()
        {
            return new HerbInfoClean
            {
                Id = Guid.NewGuid(),
                Name = string.Empty,
                Unit = "克",
                Price = 0,
                Stock = 0,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };
        }

        /// <summary>
        /// 从现有HerbInfo转换
        /// </summary>
        public static HerbInfoClean FromHerbInfo(HerbInfo herbInfo)
        {
            if (herbInfo == null)
                throw new ArgumentNullException(nameof(herbInfo));

            return new HerbInfoClean
            {
                Id = herbInfo.Id,
                Name = herbInfo.Name,
                PinYinCode = herbInfo.PinYinCode,
                Origin = herbInfo.Origin,
                Spec = herbInfo.Spec,
                Unit = herbInfo.Unit,
                Price = herbInfo.Price,
                Effect = herbInfo.Effect,
                Usage = herbInfo.Usage,
                Remark = herbInfo.Remark,
                Status = herbInfo.Status,
                Category = herbInfo.Category,
                Stock = herbInfo.Stock,
                Supplier = herbInfo.Supplier,
                LastOperationTime = herbInfo.LastOperationTime,
                OperatorName = herbInfo.OperatorName,
                StatusDescription = herbInfo.StatusDescription
            };
        }

        #endregion

        #region Business Logic Methods

        /// <summary>
        /// 检查是否库存充足
        /// </summary>
        public bool HasSufficientStock(decimal requiredAmount)
        {
            return Stock >= requiredAmount;
        }

        /// <summary>
        /// 检查是否需要库存预警
        /// </summary>
        public bool NeedsStockWarning(decimal warningThreshold = 10)
        {
            return Stock <= warningThreshold;
        }

        /// <summary>
        /// 检查是否缺货
        /// </summary>
        public bool IsOutOfStock()
        {
            return Stock <= 0;
        }

        /// <summary>
        /// 检查是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled && !IsOutOfStock();
        }

        #endregion

        #region Equality and Comparison

        /// <summary>
        /// 判断是否为同一药材
        /// </summary>
        public bool IsSameHerb(HerbInfoClean other)
        {
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// 判断是否为同一药材（通过名称和规格）
        /// </summary>
        public bool IsSameHerbByNameAndSpec(HerbInfoClean other)
        {
            return other != null && 
                   string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Spec, other.Spec, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is HerbInfoClean other && IsSameHerb(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}