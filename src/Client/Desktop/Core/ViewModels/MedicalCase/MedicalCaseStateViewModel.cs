using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase
{

    /// <summary>
    /// 医疗案例状态管理视图模型 - UltraThink架构Presentation Layer
    /// 专门处理医疗案例相关的UI状态管理，完全分离业务逻辑
    /// </summary>
    public class MedicalCaseStateViewModel : BindableBase
    {

        #region UI状态字段

        private bool _isSelected;
        private bool _isExpanded;
        private bool _isEditing;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private bool _isHighlighted;
        private bool _isStartingConsultation;
        private bool _isCompleting;
        private bool _isCancelling;
        private bool _isDeleting;

        #endregion UI状态字段

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

        /// <summary>是否正在开始看诊</summary>
        public bool IsStartingConsultation
        {
            get => _isStartingConsultation;
            set => SetProperty(ref _isStartingConsultation, value);
        }

        /// <summary>是否正在完成案例</summary>
        public bool IsCompleting
        {
            get => _isCompleting;
            set => SetProperty(ref _isCompleting, value);
        }

        /// <summary>是否正在取消案例</summary>
        public bool IsCancelling
        {
            get => _isCancelling;
            set => SetProperty(ref _isCancelling, value);
        }

        /// <summary>是否正在删除</summary>
        public bool IsDeleting
        {
            get => _isDeleting;
            set => SetProperty(ref _isDeleting, value);
        }

        #endregion UI状态属性

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
            IsStartingConsultation = false;
            IsCompleting = false;
            IsCancelling = false;
            IsDeleting = false;
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
        /// 开始看诊
        /// </summary>
        public void StartStartingConsultation()
        {
            IsStartingConsultation = true;
            ClearError();
        }

        /// <summary>
        /// 结束开始看诊
        /// </summary>
        public void EndStartingConsultation()
        {
            IsStartingConsultation = false;
        }

        /// <summary>
        /// 开始完成案例
        /// </summary>
        public void StartCompleting()
        {
            IsCompleting = true;
            ClearError();
        }

        /// <summary>
        /// 结束完成案例
        /// </summary>
        public void EndCompleting()
        {
            IsCompleting = false;
        }

        /// <summary>
        /// 开始取消案例
        /// </summary>
        public void StartCancelling()
        {
            IsCancelling = true;
            ClearError();
        }

        /// <summary>
        /// 结束取消案例
        /// </summary>
        public void EndCancelling()
        {
            IsCancelling = false;
        }

        /// <summary>
        /// 开始删除
        /// </summary>
        public void StartDeleting()
        {
            IsDeleting = true;
            ClearError();
        }

        /// <summary>
        /// 结束删除
        /// </summary>
        public void EndDeleting()
        {
            IsDeleting = false;
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
            IsStartingConsultation = false;
            IsCompleting = false;
            IsCancelling = false;
            IsDeleting = false;
        }

        #endregion 状态管理方法

        #region 状态验证

        /// <summary>
        /// 是否可以编辑
        /// </summary>
        public bool CanEdit => !IsLoading && !HasError && !IsProcessing;

        /// <summary>
        /// 是否可以选择
        /// </summary>
        public bool CanSelect => !IsLoading;

        /// <summary>
        /// 是否可以开始看诊
        /// </summary>
        public bool CanStartConsultation => !IsLoading && !IsStartingConsultation && !HasError && !IsProcessing;

        /// <summary>
        /// 是否可以完成案例
        /// </summary>
        public bool CanComplete => !IsLoading && !IsCompleting && !HasError && !IsProcessing;

        /// <summary>
        /// 是否可以取消案例
        /// </summary>
        public bool CanCancel => !IsLoading && !IsCancelling && !HasError && !IsProcessing;

        /// <summary>
        /// 是否可以删除
        /// </summary>
        public bool CanDelete => !IsLoading && !IsDeleting && !HasError && !IsProcessing;

        /// <summary>
        /// 是否忙碌状态
        /// </summary>
        public bool IsBusy => IsLoading || IsEditing || IsProcessing;

        /// <summary>
        /// 是否在处理业务操作
        /// </summary>
        public bool IsProcessing => IsStartingConsultation || IsCompleting || IsCancelling || IsDeleting;

        #endregion 状态验证

        #region 状态描述

        /// <summary>
        /// 获取当前状态描述
        /// </summary>
        public string GetCurrentStateDescription()
        {
            if (HasError)
            {
                return $"错误: {ErrorMessage}";
            }

            if (IsStartingConsultation)
            {
                return "正在开始看诊...";
            }

            if (IsCompleting)
            {
                return "正在完成案例...";
            }

            if (IsCancelling)
            {
                return "正在取消案例...";
            }

            if (IsDeleting)
            {
                return "正在删除...";
            }

            if (IsLoading)
            {
                return "加载中...";
            }

            if (IsEditing)
            {
                return "编辑中";
            }

            return "就绪";
        }

        /// <summary>
        /// 获取详细状态信息
        /// </summary>
        public string GetDetailedStateInfo()
        {
            var states = new List<string>();

            if (IsSelected)
            {
                states.Add("已选中");
            }

            if (IsExpanded)
            {
                states.Add("已展开");
            }

            if (IsEditing)
            {
                states.Add("编辑中");
            }

            if (IsLoading)
            {
                states.Add("加载中");
            }

            if (IsHighlighted)
            {
                states.Add("高亮");
            }

            if (IsStartingConsultation)
            {
                states.Add("开始看诊中");
            }

            if (IsCompleting)
            {
                states.Add("完成中");
            }

            if (IsCancelling)
            {
                states.Add("取消中");
            }

            if (IsDeleting)
            {
                states.Add("删除中");
            }

            if (HasError)
            {
                states.Add($"错误: {ErrorMessage}");
            }

            return states.Any() ? string.Join(", ", states) : "正常";
        }

        #endregion 状态描述

        #region 批量操作支持

        /// <summary>
        /// 批量选择模式
        /// </summary>
        public void EnterBatchSelectionMode()
        {
            // 在批量选择模式下禁用某些操作
            if (IsEditing)
            {
                EndEditing();
            }

            if (IsExpanded)
            {
                IsExpanded = false;
            }
        }

        /// <summary>
        /// 退出批量选择模式
        /// </summary>
        public void ExitBatchSelectionMode()
        {
            IsSelected = false;
        }

        /// <summary>
        /// 检查是否适合批量操作
        /// </summary>
        public bool IsSuitableForBatchOperation()
        {
            return !HasError && !IsProcessing && !IsLoading;
        }

        #endregion 批量操作支持
    }
}
