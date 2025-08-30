using System;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Prescriptions
{
    /// <summary>
    /// 处方状态管理视图模型 - UltraThink架构Presentation Layer
    /// 专门处理处方相关的UI状态管理，完全分离业务逻辑
    /// </summary>
    public class PrescriptionStateViewModel : BindableBase
    {
        #region UI状态字段

        private bool _isSelected;
        private bool _isExpanded;
        private bool _isEditing;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private bool _isHighlighted;
        private bool _isPrinting;
        private bool _isProcessingPayment;
        private bool _isDispensing;
        private bool _isVoiding;

        #endregion

        #region UI状态属性

        /// <summary>是否被选中</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>是否正在编辑</summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>是否有错误</summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>错误消息</summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    HasError = !string.IsNullOrEmpty(value);
                }
            }
        }

        /// <summary>是否高亮显示</summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        /// <summary>是否正在打印</summary>
        public bool IsPrinting
        {
            get => _isPrinting;
            set => SetProperty(ref _isPrinting, value);
        }

        /// <summary>是否正在处理支付</summary>
        public bool IsProcessingPayment
        {
            get => _isProcessingPayment;
            set => SetProperty(ref _isProcessingPayment, value);
        }

        /// <summary>是否正在发药</summary>
        public bool IsDispensing
        {
            get => _isDispensing;
            set => SetProperty(ref _isDispensing, value);
        }

        /// <summary>是否正在作废</summary>
        public bool IsVoiding
        {
            get => _isVoiding;
            set => SetProperty(ref _isVoiding, value);
        }

        #endregion

        #region 状态管理方法

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void ResetState()
        {
            IsSelected = false;
            IsExpanded = false;
            IsEditing = false;
            IsLoading = false;
            HasError = false;
            ErrorMessage = string.Empty;
            IsHighlighted = false;
            IsPrinting = false;
            IsProcessingPayment = false;
            IsDispensing = false;
            IsVoiding = false;
        }

        /// <summary>
        /// 开始编辑
        /// </summary>
        public void StartEditing()
        {
            IsEditing = true;
            ClearError();
        }

        /// <summary>
        /// 结束编辑
        /// </summary>
        public void EndEditing()
        {
            IsEditing = false;
        }

        /// <summary>
        /// 开始加载
        /// </summary>
        public void StartLoading()
        {
            IsLoading = true;
            ClearError();
        }

        /// <summary>
        /// 结束加载
        /// </summary>
        public void EndLoading()
        {
            IsLoading = false;
        }

        /// <summary>
        /// 开始打印
        /// </summary>
        public void StartPrinting()
        {
            IsPrinting = true;
            ClearError();
        }

        /// <summary>
        /// 结束打印
        /// </summary>
        public void EndPrinting()
        {
            IsPrinting = false;
        }

        /// <summary>
        /// 开始支付处理
        /// </summary>
        public void StartPaymentProcessing()
        {
            IsProcessingPayment = true;
            ClearError();
        }

        /// <summary>
        /// 结束支付处理
        /// </summary>
        public void EndPaymentProcessing()
        {
            IsProcessingPayment = false;
        }

        /// <summary>
        /// 开始发药
        /// </summary>
        public void StartDispensing()
        {
            IsDispensing = true;
            ClearError();
        }

        /// <summary>
        /// 结束发药
        /// </summary>
        public void EndDispensing()
        {
            IsDispensing = false;
        }

        /// <summary>
        /// 开始作废
        /// </summary>
        public void StartVoiding()
        {
            IsVoiding = true;
            ClearError();
        }

        /// <summary>
        /// 结束作废
        /// </summary>
        public void EndVoiding()
        {
            IsVoiding = false;
        }

        /// <summary>
        /// 设置错误
        /// </summary>
        public void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            EndAllProcesses();
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            IsSelected = !IsSelected;
        }

        /// <summary>
        /// 切换展开状态
        /// </summary>
        public void ToggleExpansion()
        {
            IsExpanded = !IsExpanded;
        }

        /// <summary>
        /// 切换高亮状态
        /// </summary>
        public void ToggleHighlight()
        {
            IsHighlighted = !IsHighlighted;
        }

        /// <summary>
        /// 设置为焦点状态
        /// </summary>
        public void SetFocus()
        {
            IsSelected = true;
            IsHighlighted = true;
        }

        /// <summary>
        /// 取消焦点状态
        /// </summary>
        public void ClearFocus()
        {
            IsSelected = false;
            IsHighlighted = false;
        }

        /// <summary>
        /// 结束所有进行中的处理
        /// </summary>
        public void EndAllProcesses()
        {
            IsLoading = false;
            IsPrinting = false;
            IsProcessingPayment = false;
            IsDispensing = false;
            IsVoiding = false;
        }

        #endregion

        #region 状态验证

        /// <summary>
        /// 是否可以编辑
        /// </summary>
        public bool CanEdit => !IsLoading && !HasError && !IsProcessingPayment && !IsDispensing && !IsVoiding;

        /// <summary>
        /// 是否可以选择
        /// </summary>
        public bool CanSelect => !IsLoading;

        /// <summary>
        /// 是否可以打印
        /// </summary>
        public bool CanPrint => !IsLoading && !IsPrinting && !HasError;

        /// <summary>
        /// 是否可以处理支付
        /// </summary>
        public bool CanProcessPayment => !IsLoading && !IsProcessingPayment && !HasError;

        /// <summary>
        /// 是否可以发药
        /// </summary>
        public bool CanDispense => !IsLoading && !IsDispensing && !HasError;

        /// <summary>
        /// 是否可以作废
        /// </summary>
        public bool CanVoid => !IsLoading && !IsVoiding && !HasError;

        /// <summary>
        /// 是否忙碌状态
        /// </summary>
        public bool IsBusy => IsLoading || IsEditing || IsPrinting || IsProcessingPayment || IsDispensing || IsVoiding;

        /// <summary>
        /// 是否在处理业务操作
        /// </summary>
        public bool IsProcessing => IsProcessingPayment || IsDispensing || IsVoiding;

        #endregion

        #region 状态描述

        /// <summary>
        /// 获取当前状态描述
        /// </summary>
        public string GetCurrentStateDescription()
        {
            if (HasError) return $"错误: {ErrorMessage}";
            if (IsPrinting) return "正在打印...";
            if (IsProcessingPayment) return "正在处理支付...";
            if (IsDispensing) return "正在发药...";
            if (IsVoiding) return "正在作废...";
            if (IsLoading) return "加载中...";
            if (IsEditing) return "编辑中";
            return "就绪";
        }

        #endregion
    }
}