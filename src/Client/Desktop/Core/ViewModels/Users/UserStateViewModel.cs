using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Users
{
    /// <summary>
    /// 用户UI状态视图模型 - UltraThink架构的状态管理层
    /// 负责纯UI状态管理，不包含业务逻辑
    /// </summary>
    public class UserStateViewModel : BindableBase
    {
        #region UI State Properties

        private bool _isSelected = false;
        /// <summary>是否被选中（用于批量操作）</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isExpanded = false;
        /// <summary>是否展开详细信息</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isEditing = false;
        /// <summary>是否正在编辑模式</summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private bool _isLoading = false;
        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isProcessing = false;
        /// <summary>是否正在处理操作（例如保存、删除等）</summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private bool _hasError = false;
        /// <summary>是否有错误状态</summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>错误消息</summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        private bool _isHighlighted = false;
        /// <summary>是否高亮显示（用于搜索结果等）</summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        #endregion

        #region Composite State Properties

        /// <summary>是否繁忙状态（加载中或处理中）</summary>
        public bool IsBusy => IsLoading || IsProcessing;

        /// <summary>是否可以交互（非繁忙且无错误）</summary>
        public bool CanInteract => !IsBusy && !HasError;

        /// <summary>是否在活动状态（选中、编辑或展开）</summary>
        public bool IsActive => IsSelected || IsEditing || IsExpanded;

        #endregion

        #region State Management Methods

        /// <summary>
        /// 开始编辑模式
        /// </summary>
        public void StartEditing()
        {
            IsEditing = true;
            IsExpanded = true;
            ClearError();
        }

        /// <summary>
        /// 结束编辑模式
        /// </summary>
        public void StopEditing()
        {
            IsEditing = false;
        }

        /// <summary>
        /// 开始加载状态
        /// </summary>
        public void StartLoading()
        {
            IsLoading = true;
            ClearError();
        }

        /// <summary>
        /// 结束加载状态
        /// </summary>
        public void StopLoading()
        {
            IsLoading = false;
        }

        /// <summary>
        /// 开始处理状态
        /// </summary>
        public void StartProcessing()
        {
            IsProcessing = true;
            ClearError();
        }

        /// <summary>
        /// 结束处理状态
        /// </summary>
        public void StopProcessing()
        {
            IsProcessing = false;
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
        /// 设置错误状态
        /// </summary>
        public void SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
            IsLoading = false;
            IsProcessing = false;
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// 重置所有状态到初始值
        /// </summary>
        public void Reset()
        {
            IsSelected = false;
            IsExpanded = false;
            IsEditing = false;
            IsLoading = false;
            IsProcessing = false;
            IsHighlighted = false;
            ClearError();
        }

        /// <summary>
        /// 重置除选中状态外的所有状态
        /// </summary>
        public void ResetExceptSelection()
        {
            IsExpanded = false;
            IsEditing = false;
            IsLoading = false;
            IsProcessing = false;
            IsHighlighted = false;
            ClearError();
        }

        #endregion
    }
}