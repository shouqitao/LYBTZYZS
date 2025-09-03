using System;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Patients
{
    /// <summary>
    /// 患者UI状态视图模型 - UltraThink架构的状态层
    /// 负责纯UI状态管理，不涉及业务逻辑
    /// </summary>
    public class PatientStateViewModel : BindableBase
    {
        #region Fields

        private bool _isSelected;
        private bool _isEditing;
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private bool _isExpanded;
        private bool _isHighlighted;

        #endregion

        #region Selection State

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        #endregion

        #region Edit State

        /// <summary>是否编辑中</summary>
        public bool IsEditing
        {
            get => _isEditing;
            private set => SetProperty(ref _isEditing, value);
        }

        #endregion

        #region Loading State

        /// <summary>是否加载中</summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Error State

        /// <summary>是否有错误</summary>
        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        /// <summary>错误信息</summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        #endregion

        #region UI State

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>是否高亮</summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        #endregion

        #region State Control Methods

        /// <summary>
        /// 开始编辑
        /// </summary>
        public void StartEditing()
        {
            IsEditing = true;
            ClearError();
        }

        /// <summary>
        /// 停止编辑
        /// </summary>
        public void StopEditing()
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
        /// 停止加载
        /// </summary>
        public void StopLoading()
        {
            IsLoading = false;
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            IsSelected = !IsSelected;
        }

        /// <summary>
        /// 设置错误
        /// </summary>
        public void SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
            HasError = !string.IsNullOrWhiteSpace(errorMessage);
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = null;
            HasError = false;
        }

        /// <summary>
        /// 切换展开状态
        /// </summary>
        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void Reset()
        {
            IsSelected = false;
            IsEditing = false;
            IsLoading = false;
            IsExpanded = false;
            IsHighlighted = false;
            ClearError();
        }

        #endregion
    }
}