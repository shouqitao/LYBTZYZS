using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类 - 编辑状态与资源释放部分
    ///
    /// 包含:
    /// - 消息对话框方法 (Toast/确认对话框)
    /// - 未保存变更追踪辅助方法
    /// - IEditable编辑状态实现
    /// - IDisposable资源释放
    /// </summary>
    public abstract partial class NavigableViewModelBase
    {
        #region 对话框方法

        /// <summary>
        /// 显示成功消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowSuccessMessageAsync(string message)
        {
            ToastService.ShowSuccess(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示错误消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowErrorMessageAsync(string message)
        {
            ToastService.ShowError(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowWarningMessageAsync(string message)
        {
            ToastService.ShowWarning(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认")
        {
            return await CommonDialogService.ShowConfirmAsync(message, title);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 标记有未保存的变更
        /// </summary>
        protected void MarkAsChanged()
        {
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// 标记变更已保存
        /// </summary>
        protected void MarkAsSaved()
        {
            HasUnsavedChanges = false;
        }

        #endregion

        #region IEditable Explicit Implementation

        void IEditable.MarkAsChanged() => MarkAsChanged();
        void IEditable.MarkAsSaved() => MarkAsSaved();

        #endregion

        #region IEditable Implementation

        /// <summary>
        /// 开始编辑
        /// </summary>
        public virtual void BeginEdit()
        {
            IsEditing = true;
            Logger.LogDebug("开始编辑: {ViewType}", GetType().Name);
            OnBeginEdit();
        }

        /// <summary>
        /// 取消编辑（恢复原始值）
        /// </summary>
        public virtual void CancelEdit()
        {
            IsEditing = false;
            HasUnsavedChanges = false;
            Logger.LogDebug("取消编辑: {ViewType}", GetType().Name);
            OnCancelEdit();
        }

        /// <summary>
        /// 确认编辑完成
        /// </summary>
        public virtual void EndEdit()
        {
            IsEditing = false;
            HasUnsavedChanges = false;
            Logger.LogDebug("结束编辑: {ViewType}", GetType().Name);
            OnEndEdit();
        }

        /// <summary>
        /// 开始编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnBeginEdit() { }

        /// <summary>
        /// 取消编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnCancelEdit() { }

        /// <summary>
        /// 结束编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnEndEdit() { }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _eventManager?.Dispose();
                _disposables.Dispose();
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 子类可重写以执行清理逻辑
        /// </summary>
        protected virtual void OnDisposing() { }

        #endregion
    }
}
