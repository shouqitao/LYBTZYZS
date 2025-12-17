using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 处方药材项ViewModel - 继承HerbItemViewModelBase
    /// Epic #2175 BF-002 Task 3.5 - 处方药材项数据模型
    /// Bug Fix: 继承HerbItemViewModelBase以复用拼音码过滤逻辑
    /// </summary>
    public class PrescriptionItemViewModel : HerbItemViewModelBase
    {
        #region 字段

        private decimal _unitPrice;
        private decimal _itemAmount;
        private bool _isDosageValid = true;
        private string _dosageValidationMessage = string.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 单价（元/克）- 实现基类抽象属性
        /// </summary>
        public override decimal UnitPrice => _unitPrice;

        /// <summary>
        /// 设置单价（内部使用）
        /// </summary>
        public void SetUnitPrice(decimal value)
        {
            if (_unitPrice != value)
            {
                _unitPrice = value;
                RaisePropertyChanged(nameof(UnitPrice));
                CalculateAmount();
            }
        }

        /// <summary>
        /// 小计金额（剂量 × 单价）
        /// </summary>
        public decimal ItemAmount
        {
            get => _itemAmount;
            private set => SetProperty(ref _itemAmount, value);
        }

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

        /// <summary>
        /// 可选煎法列表 - 用于UI下拉绑定
        /// </summary>
        public static IReadOnlyList<DecocteMethod> AvailableDecocteMethods { get; } =
            Enum.GetValues<DecocteMethod>().ToList().AsReadOnly();

        #endregion

        #region 重写钩子方法

        /// <summary>
        /// 药材选中后 - 获取单价并计算金额
        /// </summary>
        protected override void OnHerbSelected(HerbDto herb)
        {
            base.OnHerbSelected(herb);
            SetUnitPrice(herb.Price);
        }

        /// <summary>
        /// 剂量变更后 - 验证并重算金额
        /// </summary>
        protected override void OnDosageChanged(int newDosage)
        {
            base.OnDosageChanged(newDosage);
            ValidateDosage();
            CalculateAmount();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证剂量范围
        /// 标准范围：1g - 500g（空行Dosage=0不参与验证）
        /// </summary>
        private void ValidateDosage()
        {
            const int MinDosage = 1;
            const int MaxDosage = 500;

            // 空行（未选药材）不验证
            if (HerbId == Guid.Empty)
            {
                IsDosageValid = true;
                DosageValidationMessage = string.Empty;
                return;
            }

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

        /// <summary>
        /// 计算小计金额
        /// </summary>
        private void CalculateAmount()
        {
            ItemAmount = Dosage * _unitPrice;
        }

        #endregion
    }
}
