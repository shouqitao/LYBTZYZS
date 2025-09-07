using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Prescriptions
{

    /// <summary>
    /// 处方协调视图模型 - UltraThink架构Business Layer
    /// 组合Display、State、Theme三个ViewModel，实现完整的处方视图逻辑
    /// 遵循单一职责原则和关注点分离
    /// </summary>
    public class PrescriptionViewModel : BindableBase
    {

        #region Fields

        private readonly PrescriptionDto _prescriptionData;
        private readonly PrescriptionDisplayViewModel _display;
        private readonly PrescriptionStateViewModel _state;
        private readonly PrescriptionThemeViewModel _theme;

        #endregion Fields

        #region Constructor

        private PrescriptionViewModel(PrescriptionDto prescriptionData)
        {
            _prescriptionData = prescriptionData ?? throw new ArgumentNullException(nameof(prescriptionData));

            // 初始化三个专门的ViewModel
            _display = new PrescriptionDisplayViewModel(_prescriptionData);
            _state = new PrescriptionStateViewModel();
            _theme = new PrescriptionThemeViewModel(_prescriptionData);

            // 监听状态变化以便通知UI更新
            _state.PropertyChanged += OnStatePropertyChanged;
        }

        #endregion Constructor

        #region Factory Method

        /// <summary>
        /// 创建处方视图模型实例
        /// </summary>
        public static PrescriptionViewModel Create(PrescriptionDto prescriptionData)
        {
            return new PrescriptionViewModel(prescriptionData);
        }

        #endregion Factory Method

        #region Core Properties

        /// <summary>处方业务数据（只读）</summary>
        public PrescriptionDto PrescriptionData => _prescriptionData;

        /// <summary>显示逻辑视图模型</summary>
        public PrescriptionDisplayViewModel Display => _display;

        /// <summary>状态管理视图模型</summary>
        public PrescriptionStateViewModel State => _state;

        /// <summary>主题样式视图模型</summary>
        public PrescriptionThemeViewModel Theme => _theme;

        #endregion Core Properties

        #region Convenience Properties

        /// <summary>处方ID</summary>
        public Guid Id => _prescriptionData.Id;

        /// <summary>处方编号显示</summary>
        public string PrescriptionNumber => _display.PrescriptionNumberDisplay;

        /// <summary>患者姓名显示</summary>
        public string PatientName => _display.PatientNameDisplay;

        /// <summary>医生姓名显示</summary>
        public string DoctorName => _display.DoctorNameDisplay;

        /// <summary>显示名称（用于列表显示）</summary>
        public string DisplayName => $"{PrescriptionNumber} - {PatientName}";

        /// <summary>状态显示</summary>
        public string StatusDisplay => _display.StatusDisplay;

        /// <summary>总金额显示</summary>
        public string TotalAmountDisplay => _display.TotalAmountDisplay;

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => _display.CreateTimeDisplay;

        /// <summary>总价格（计算属性）</summary>
        public decimal TotalPrice => _prescriptionData.TotalPrice;

        /// <summary>折扣金额（计算属性）</summary>
        public decimal DiscountAmount => _prescriptionData.SingleDosePrice * _prescriptionData.DosageCount * (1 - _prescriptionData.Discount);

        /// <summary>应付金额（计算属性）</summary>
        public decimal PayableAmount => _prescriptionData.TotalPrice;

        /// <summary>折扣率（计算属性）</summary>
        public decimal DiscountRate => (1 - _prescriptionData.Discount) * 100;

        /// <summary>单剂价格（计算属性）</summary>
        public decimal SingleDosePrice => _prescriptionData.SingleDosePrice;

        /// <summary>药材数量</summary>
        public int HerbCount => _prescriptionData.Items?.Count ?? 0;

        /// <summary>剂数</summary>
        public int DosageCount => _prescriptionData.DosageCount;

        #endregion Convenience Properties

        #region State Convenience Properties

        /// <summary>是否被选中</summary>
        public bool IsSelected
        {
            get => _state.IsSelected;
            set => _state.IsSelected = value;
        }

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => _state.IsExpanded;
            set => _state.IsExpanded = value;
        }

        /// <summary>是否正在编辑</summary>
        public bool IsEditing
        {
            get => _state.IsEditing;
            set => _state.IsEditing = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _state.IsLoading;
            set => _state.IsLoading = value;
        }

        /// <summary>是否有错误</summary>
        public bool HasError
        {
            get => _state.HasError;
            set => _state.HasError = value;
        }

        /// <summary>错误消息</summary>
        public string ErrorMessage
        {
            get => _state.ErrorMessage;
            set => _state.ErrorMessage = value;
        }

        /// <summary>是否高亮显示</summary>
        public bool IsHighlighted
        {
            get => _state.IsHighlighted;
            set => _state.IsHighlighted = value;
        }

        #endregion State Convenience Properties

        #region Prescription Specific State

        /// <summary>是否正在打印</summary>
        public bool IsPrinting
        {
            get => _state.IsPrinting;
            set => _state.IsPrinting = value;
        }

        /// <summary>是否正在处理支付</summary>
        public bool IsProcessingPayment
        {
            get => _state.IsProcessingPayment;
            set => _state.IsProcessingPayment = value;
        }

        /// <summary>是否正在发药</summary>
        public bool IsDispensing
        {
            get => _state.IsDispensing;
            set => _state.IsDispensing = value;
        }

        /// <summary>是否正在作废</summary>
        public bool IsVoiding
        {
            get => _state.IsVoiding;
            set => _state.IsVoiding = value;
        }

        #endregion Prescription Specific State

        #region Business Logic Convenience Methods

        /// <summary>
        /// 开始编辑处方
        /// </summary>
        public void StartEditing()
        {
            _state.StartEditing();
        }

        /// <summary>
        /// 结束编辑处方
        /// </summary>
        public void EndEditing()
        {
            _state.EndEditing();
        }

        /// <summary>
        /// 开始打印处方
        /// </summary>
        public void StartPrinting()
        {
            _state.StartPrinting();
        }

        /// <summary>
        /// 结束打印处方
        /// </summary>
        public void EndPrinting()
        {
            _state.EndPrinting();
        }

        /// <summary>
        /// 开始支付处理
        /// </summary>
        public void StartPaymentProcessing()
        {
            _state.StartPaymentProcessing();
        }

        /// <summary>
        /// 结束支付处理
        /// </summary>
        public void EndPaymentProcessing()
        {
            _state.EndPaymentProcessing();
        }

        /// <summary>
        /// 开始发药
        /// </summary>
        public void StartDispensing()
        {
            _state.StartDispensing();
        }

        /// <summary>
        /// 结束发药
        /// </summary>
        public void EndDispensing()
        {
            _state.EndDispensing();
        }

        /// <summary>
        /// 开始作废
        /// </summary>
        public void StartVoiding()
        {
            _state.StartVoiding();
        }

        /// <summary>
        /// 结束作废
        /// </summary>
        public void EndVoiding()
        {
            _state.EndVoiding();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string message)
        {
            _state.SetError(message);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            _state.ClearError();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            _state.ToggleSelection();
        }

        /// <summary>
        /// 切换展开状态
        /// </summary>
        public void ToggleExpansion()
        {
            _state.ToggleExpansion();
        }

        /// <summary>
        /// 设置为焦点状态
        /// </summary>
        public void SetFocus()
        {
            _state.SetFocus();
        }

        /// <summary>
        /// 取消焦点状态
        /// </summary>
        public void ClearFocus()
        {
            _state.ClearFocus();
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void ResetState()
        {
            _state.ResetState();
        }

        #endregion Business Logic Convenience Methods

        #region Display Convenience Methods

        /// <summary>
        /// 获取处方摘要信息
        /// </summary>
        public string GetSummaryInfo()
        {
            return _display.GetSummaryInfo();
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            return _display.GetDetailedInfo();
        }

        /// <summary>
        /// 获取打印用格式化文本
        /// </summary>
        public string GetPrintableInfo()
        {
            return _display.GetPrintableInfo();
        }

        /// <summary>
        /// 获取状态徽章文本
        /// </summary>
        public string GetStatusBadge()
        {
            return _display.GetStatusBadge();
        }

        /// <summary>
        /// 获取优先级显示
        /// </summary>
        public string GetPriorityDisplay()
        {
            return _display.GetPriorityDisplay();
        }

        #endregion Display Convenience Methods

        #region Business Data Convenience Methods

        /// <summary>
        /// 检查处方是否包含指定药材 - UltraThink v2.0 简化实现
        /// </summary>
        public bool ContainsHerb(string herbName)
        {
            return _prescriptionData.Items?.Any(item => item.HerbName?.Contains(herbName, StringComparison.OrdinalIgnoreCase) == true) ?? false;
        }

        /// <summary>
        /// 获取指定药材的数量 - UltraThink v2.0 简化实现
        /// </summary>
        public decimal GetHerbQuantity(string herbName)
        {
            return _prescriptionData.Items?.FirstOrDefault(item => item.HerbName?.Equals(herbName, StringComparison.OrdinalIgnoreCase) == true)?.Quantity ?? 0;
        }

        /// <summary>
        /// 检查处方是否已完成 - 简化为启用状态表示已完成
        /// </summary>
        public bool IsCompleted => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>
        /// 检查处方是否可以发药 - 简化为启用状态表示可以发药
        /// </summary>
        public bool CanDispense => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>
        /// 检查处方是否需要支付 - 简化为禁用状态表示需要支付
        /// </summary>
        public bool NeedsPayment => _prescriptionData.Status == CommonStatus.Disabled;

        /// <summary>
        /// 获取处方完成度百分比 - UltraThink v2.0 简化逻辑
        /// </summary>
        public double GetCompletionPercentage()
        {
            return _prescriptionData.Status switch
            {
                CommonStatus.Enabled => 100.0, // 启用状态表示完成
                CommonStatus.Disabled => 50.0, // 禁用状态表示进行中
                _ => 0.0
            };
        }

        /// <summary>
        /// 格式化总价格显示
        /// </summary>
        public string FormatTotalPrice()
        {
            return TotalPrice.ToString("F2") + "元";
        }

        /// <summary>
        /// 格式化折扣金额显示
        /// </summary>
        public string FormatDiscountAmount()
        {
            return DiscountAmount > 0 ? "-" + DiscountAmount.ToString("F2") + "元" : "无折扣";
        }

        /// <summary>
        /// 格式化应付金额显示
        /// </summary>
        public string FormatPayableAmount()
        {
            return PayableAmount.ToString("F2") + "元";
        }

        /// <summary>
        /// 格式化单剂价格显示
        /// </summary>
        public string FormatSingleDosePrice()
        {
            return SingleDosePrice.ToString("F2") + "元/剂";
        }

        /// <summary>
        /// 格式化折扣率显示
        /// </summary>
        public string FormatDiscountRate()
        {
            return DiscountRate > 0 ? (DiscountRate * 100).ToString("F1") + "%" : "无折扣";
        }

        /// <summary>
        /// 获取价格摘要信息
        /// </summary>
        public string GetPricingSummary()
        {
            if (DiscountAmount > 0)
            {
                return $"{HerbCount}味药材，{DosageCount}剂，原价{FormatTotalPrice()}，折扣{FormatDiscountAmount()}，实付{FormatPayableAmount()}";
            }
            else
            {
                return $"{HerbCount}味药材，{DosageCount}剂，总价{FormatTotalPrice()}";
            }
        }

        /// <summary>
        /// 检查是否有折扣
        /// </summary>
        public bool HasDiscount => DiscountAmount > 0;

        /// <summary>
        /// 计算节省金额
        /// </summary>
        public decimal CalculateSavings()
        {
            return TotalPrice - PayableAmount;
        }

        #endregion Business Data Convenience Methods

        #region Event Handling

        private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当State的属性改变时，通知相关的便利属性也已改变
            switch (e.PropertyName)
            {
                case nameof(PrescriptionStateViewModel.IsSelected):
                    RaisePropertyChanged(nameof(IsSelected));
                    break;

                case nameof(PrescriptionStateViewModel.IsExpanded):
                    RaisePropertyChanged(nameof(IsExpanded));
                    break;

                case nameof(PrescriptionStateViewModel.IsEditing):
                    RaisePropertyChanged(nameof(IsEditing));
                    break;

                case nameof(PrescriptionStateViewModel.IsLoading):
                    RaisePropertyChanged(nameof(IsLoading));
                    break;

                case nameof(PrescriptionStateViewModel.HasError):
                    RaisePropertyChanged(nameof(HasError));
                    break;

                case nameof(PrescriptionStateViewModel.ErrorMessage):
                    RaisePropertyChanged(nameof(ErrorMessage));
                    break;

                case nameof(PrescriptionStateViewModel.IsHighlighted):
                    RaisePropertyChanged(nameof(IsHighlighted));
                    break;

                case nameof(PrescriptionStateViewModel.IsPrinting):
                    RaisePropertyChanged(nameof(IsPrinting));
                    break;

                case nameof(PrescriptionStateViewModel.IsProcessingPayment):
                    RaisePropertyChanged(nameof(IsProcessingPayment));
                    break;

                case nameof(PrescriptionStateViewModel.IsDispensing):
                    RaisePropertyChanged(nameof(IsDispensing));
                    break;

                case nameof(PrescriptionStateViewModel.IsVoiding):
                    RaisePropertyChanged(nameof(IsVoiding));
                    break;
            }
        }

        #endregion Event Handling

        #region Object Overrides

        public override string ToString()
        {
            return $"PrescriptionViewModel: {DisplayName} ({StatusDisplay})";
        }

        public override bool Equals(object? obj)
        {
            return obj is PrescriptionViewModel other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion Object Overrides

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消事件订阅
                    _state.PropertyChanged -= OnStatePropertyChanged;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
