using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 带验证功能的ViewModel基类
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 继承自ObservableValidator，提供:
    /// - INotifyDataErrorInfo实现
    /// - 属性验证支持
    /// - DataAnnotation验证集成
    /// </summary>
    public abstract partial class ValidatingViewModelBase : ObservableValidator, IDisposable
    {
        #region 私有字段

        private readonly CompositeDisposable _disposables = new();
        private bool _disposed;
        private readonly Dictionary<string, List<string>> _customErrors = new();

        #endregion

        #region 受保护属性

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 日志记录器工厂
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 错误消息（全局错误，非字段级验证错误）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否未在忙碌状态
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// 是否有全局错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 是否有任何验证错误（包括字段级和全局）
        /// </summary>
        public bool HasAnyErrors => HasErrors || HasError || _customErrors.Any();

        #endregion

        #region 构造函数

        protected ValidatingViewModelBase(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());
        }

        #endregion

        #region 属性变更回调

        /// <summary>
        /// IsBusy属性变更时调用
        /// </summary>
        partial void OnIsBusyChanged(bool value)
        {
            OnIsBusyChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应IsBusy变更
        /// </summary>
        protected virtual void OnIsBusyChangedCore(bool value) { }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证指定属性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="value">属性值</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>验证是否通过</returns>
        protected bool ValidatePropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return true;

            ClearErrors(propertyName);
            ValidateProperty(value, propertyName);
            return !GetErrors(propertyName).Cast<object>().Any();
        }

        /// <summary>
        /// 验证所有属性
        /// </summary>
        /// <returns>验证是否全部通过</returns>
        protected bool ValidateAllPropertiesAndCheck()
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        /// <summary>
        /// 添加自定义验证错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="errorMessage">错误消息</param>
        protected void AddCustomError(string propertyName, string errorMessage)
        {
            if (!_customErrors.TryGetValue(propertyName, out var errors))
            {
                errors = new List<string>();
                _customErrors[propertyName] = errors;
            }

            if (!errors.Contains(errorMessage))
            {
                errors.Add(errorMessage);
                NotifyErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 清除自定义验证错误
        /// </summary>
        /// <param name="propertyName">属性名，null则清除所有</param>
        protected void ClearCustomErrors(string? propertyName = null)
        {
            if (propertyName == null)
            {
                var names = _customErrors.Keys.ToList();
                _customErrors.Clear();
                foreach (var name in names)
                {
                    NotifyErrorsChanged(name);
                }
            }
            else if (_customErrors.ContainsKey(propertyName))
            {
                _customErrors.Remove(propertyName);
                NotifyErrorsChanged(propertyName);
            }
            else
            {
                OnPropertyChanged(nameof(HasAnyErrors));
            }
        }

        /// <summary>
        /// 获取所有错误（包括DataAnnotation和自定义错误）
        /// </summary>
        public IEnumerable GetAllErrors(string? propertyName)
        {
            var baseErrors = GetErrors(propertyName);

            if (string.IsNullOrEmpty(propertyName))
            {
                return baseErrors.Cast<object>()
                    .Concat(_customErrors.SelectMany(x => x.Value));
            }

            if (_customErrors.TryGetValue(propertyName, out var customErrors))
            {
                return baseErrors.Cast<object>().Concat(customErrors);
            }

            return baseErrors;
        }

        /// <summary>
        /// 触发错误变更事件（通过属性变更通知）
        /// </summary>
        protected void NotifyErrorsChanged(string propertyName)
        {
            // ObservableValidator内部管理ErrorsChanged事件
            // 这里通过通知HasErrors和HasAnyErrors属性变更来触发UI更新
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(HasAnyErrors));
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        protected void SetBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
            else if (!isBusy)
            {
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 设置错误消息
        /// </summary>
        protected void SetError(string message)
        {
            ErrorMessage = message;
            Logger.LogWarning("设置错误消息: {Message}", message);
        }

        /// <summary>
        /// 清除所有错误（包括验证错误和全局错误）
        /// </summary>
        protected void ClearAllErrors()
        {
            ClearErrors();
            ClearCustomErrors();
            ClearError();
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                return action();
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        #endregion

        #region Disposable管理

        /// <summary>
        /// 添加可释放对象到管理集合
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

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
