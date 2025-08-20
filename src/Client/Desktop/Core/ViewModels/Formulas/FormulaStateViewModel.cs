using System;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Formulas
{
    /// <summary>
    /// 验方状态管理视图模型 - UltraThink架构Presentation Layer
    /// 专门处理验方相关的UI状态管理，完全分离业务逻辑
    /// </summary>
    public class FormulaStateViewModel : BindableBase
    {
        #region UI状态字段

        private bool _isSelected;
        private bool _isExpanded;
        private bool _isEditing;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private bool _isHighlighted;
        private bool _isFavorite;

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

        /// <summary>是否为收藏</summary>
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
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
        /// 设置错误
        /// </summary>
        public void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            IsLoading = false;
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
        /// 切换收藏状态
        /// </summary>
        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
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

        #endregion

        #region 状态验证

        /// <summary>
        /// 是否可以编辑
        /// </summary>
        public bool CanEdit => !IsLoading && !HasError;

        /// <summary>
        /// 是否可以选择
        /// </summary>
        public bool CanSelect => !IsLoading;

        /// <summary>
        /// 是否忙碌状态
        /// </summary>
        public bool IsBusy => IsLoading || IsEditing;

        #endregion
    }
}