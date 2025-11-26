using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 处方药材项ViewModel - 继承HerbItemViewModelBase并提供价格计算功能
    /// Issue: unify-herb-card-control - 统一处方和经验方的药材编辑体验
    /// </summary>
    public class PrescriptionHerbItemViewModel : HerbItemViewModelBase
    {
        #region 字段

        private decimal _itemTotal;
        private decimal _loadedUnitPrice; // 从DTO加载的价格
        private bool _isDosageValid = true;
        private string _dosageValidationMessage = string.Empty;

        #endregion

        #region 价格属性

        /// <summary>
        /// 单价（元/克）- 从药材库获取实际价格或从DTO加载的价格
        /// 重写基类抽象属性
        /// </summary>
        public override decimal UnitPrice => SelectedHerb?.Price ?? _loadedUnitPrice;

        /// <summary>
        /// 设置从DTO加载的单价（用于LoadFromDto场景）
        /// </summary>
        public void SetLoadedUnitPrice(decimal price)
        {
            _loadedUnitPrice = price;
            RaisePropertyChanged(nameof(UnitPrice));
            CalculateItemTotal();
        }

        /// <summary>
        /// 小计金额（剂量 x 单价）
        /// </summary>
        public decimal ItemTotal
        {
            get => _itemTotal;
            private set => SetProperty(ref _itemTotal, value);
        }

        #endregion

        #region 验证属性

        /// <summary>
        /// 剂量是否有效（用于UI验证提示）
        /// </summary>
        public bool IsDosageValid
        {
            get => _isDosageValid;
            set => SetProperty(ref _isDosageValid, value);
        }

        /// <summary>
        /// 剂量验证错误消息
        /// </summary>
        public string DosageValidationMessage
        {
            get => _dosageValidationMessage;
            set => SetProperty(ref _dosageValidationMessage, value);
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 药材选中后的回调 - 更新价格相关属性
        /// </summary>
        protected override void OnHerbSelected(HerbDto herb)
        {
            // 通知UnitPrice属性变更
            RaisePropertyChanged(nameof(UnitPrice));
            CalculateItemTotal();
        }

        /// <summary>
        /// 剂量变更后的回调 - 更新价格计算
        /// </summary>
        protected override void OnDosageChanged(decimal newDosage)
        {
            ValidateDosage();
            CalculateItemTotal();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算小计金额
        /// </summary>
        private void CalculateItemTotal()
        {
            ItemTotal = Dosage * UnitPrice;
        }

        /// <summary>
        /// 验证剂量范围
        /// 标准范围：0.1g - 500g
        /// </summary>
        private void ValidateDosage()
        {
            const decimal MinDosage = 0.1m;
            const decimal MaxDosage = 500m;

            if (Dosage < MinDosage)
            {
                IsDosageValid = false;
                DosageValidationMessage = $"剂量不能小于{MinDosage}g";
            }
            else if (Dosage > MaxDosage)
            {
                IsDosageValid = false;
                DosageValidationMessage = $"剂量不能大于{MaxDosage}g";
            }
            else
            {
                IsDosageValid = true;
                DosageValidationMessage = string.Empty;
            }
        }

        #endregion
    }
}
