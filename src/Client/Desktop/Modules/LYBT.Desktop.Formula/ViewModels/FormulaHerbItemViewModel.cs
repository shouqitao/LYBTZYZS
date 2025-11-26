using LYBT.Desktop.Models.ViewModels.Base;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 经验方药材项ViewModel - 继承HerbItemViewModelBase
    /// Phase 3: 复用共享基类的拼音码过滤逻辑
    /// Issue: unify-herb-card-control - 统一经验方和处方的药材编辑体验
    /// </summary>
    public class FormulaHerbItemViewModel : HerbItemViewModelBase
    {
        #region 字段

        private string? _remark;

        #endregion

        #region 属性

        /// <summary>
        /// 备注（加工方法等）
        /// </summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 单价 - 经验方模块不涉及价格，固定返回0
        /// </summary>
        public override decimal UnitPrice => 0m;

        #endregion

        #region 公共方法

        /// <summary>
        /// 转换为DTO用于保存
        /// </summary>
        public LYBT.Shared.Models.Contracts.Formula.FormulaHerbItemInputDto ToDto()
        {
            return new LYBT.Shared.Models.Contracts.Formula.FormulaHerbItemInputDto
            {
                HerbId = HerbId == Guid.Empty ? null : HerbId,
                HerbName = HerbName,
                Quantity = Dosage,
                Unit = Unit,
                ProcessingMethod = Remark
            };
        }

        #endregion
    }
}
