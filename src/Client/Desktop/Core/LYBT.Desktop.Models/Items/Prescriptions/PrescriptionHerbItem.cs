using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Models.Items.Prescriptions;

/// <summary>
/// 处方药材项 - 继承HerbItemViewModelBase
/// OpenSpec: unify-frontend-backend-types Phase 8.4
/// 统一自PrescriptionItemViewModel和PrescriptionHerbItemViewModel
/// 用于医案处方编辑、处方模板等场景
/// </summary>
public class PrescriptionHerbItem : HerbItemViewModelBase
{
    #region 字段

    private decimal _unitPrice;
    private decimal _itemTotal;
    private bool _isDosageValid = true;
    private string _dosageValidationMessage = string.Empty;

    #endregion

    #region 价格属性

    /// <summary>
    /// 单价（元/克）- 实现基类抽象属性
    /// </summary>
    public override decimal UnitPrice => _unitPrice;

    /// <summary>
    /// 设置单价（用于从药材选择或DTO加载）
    /// </summary>
    public void SetUnitPrice(decimal value)
    {
        if (_unitPrice != value)
        {
            _unitPrice = value;
            RaisePropertyChanged(nameof(UnitPrice));
            CalculateItemTotal();
        }
    }

    /// <summary>
    /// 设置从DTO加载的单价
    /// OpenSpec: unify-frontend-backend-types Phase 8 - 兼容别名方法
    /// </summary>
    public void SetLoadedUnitPrice(decimal value) => SetUnitPrice(value);

    /// <summary>
    /// 小计金额（剂量 × 单价）
    /// </summary>
    public decimal ItemTotal
    {
        get => _itemTotal;
        private set => SetProperty(ref _itemTotal, value);
    }

    /// <summary>
    /// 小计金额（ItemTotal的兼容别名）
    /// OpenSpec: unify-frontend-backend-types Phase 8 - 向后兼容
    /// </summary>
    public decimal ItemAmount => ItemTotal;

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

    #region 静态属性

    /// <summary>
    /// 可选煎法列表 - 用于UI下拉绑定
    /// </summary>
    public static IReadOnlyList<DecocteMethod> AvailableDecocteMethods { get; } =
        Enum.GetValues<DecocteMethod>().ToList().AsReadOnly();

    #endregion

    #region 重写基类方法

    /// <summary>
    /// 药材选中后 - 获取单价并计算金额
    /// </summary>
    protected override void OnHerbSelected(HerbDetailDto herb)
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
        CalculateItemTotal();
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
    private void CalculateItemTotal()
    {
        ItemTotal = Dosage * _unitPrice;
    }

    #endregion
}
