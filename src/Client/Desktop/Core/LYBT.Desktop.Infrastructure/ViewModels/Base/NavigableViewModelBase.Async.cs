using LYBT.Desktop.Foundation.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类 - 异步执行包装部分
    ///
    /// 包含统一异常处理的异步执行包装与UI线程调度方法
    /// </summary>
    public abstract partial class NavigableViewModelBase
    {
        #region 异步执行包装

        /// <summary>
        /// 异步执行包装 - 统一异常处理
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志和错误消息）</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        protected async Task ExecuteWithErrorHandlingAsync(
            Func<Task> action,
            string operationName,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operationName, ex));
                }
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        /// <summary>
        /// 异步执行包装 (带返回值)
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">失败时的默认值</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        /// <returns>操作结果或默认值</returns>
        protected async Task<T?> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> action,
            string operationName,
            T? defaultValue = default,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                return await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operationName, ex));
                }
                return defaultValue;
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            UiDispatcher.Invoke(action);
        }

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            return UiDispatcher.InvokeAsync(action);
        }

        #endregion
    }
}
