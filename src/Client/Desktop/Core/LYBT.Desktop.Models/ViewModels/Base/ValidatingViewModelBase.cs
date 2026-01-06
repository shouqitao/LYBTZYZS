using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 带验证功能的ViewModel基类
    /// OpenSpec: standardize-viewmodel-framework
    ///
    /// 继承CoreViewModelBase，实现INotifyDataErrorInfo:
    /// - 属性级验证错误
    /// - FluentValidation集成
    /// - 自定义验证错误
    /// - 验证状态追踪
    /// </summary>
    public abstract partial class ValidatingViewModelBase : CoreViewModelBase, INotifyDataErrorInfo
    {
        #region 私有字段

        private readonly Dictionary<string, List<string>> _errors = new();

        #endregion

        #region INotifyDataErrorInfo

        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasErrors => _errors.Any(e => e.Value.Count > 0);

        /// <summary>
        /// 错误变更事件
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// 获取指定属性的错误
        /// </summary>
        /// <param name="propertyName">属性名，null返回所有错误</param>
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.SelectMany(e => e.Value);
            }

            return _errors.TryGetValue(propertyName, out var errors)
                ? errors
                : Enumerable.Empty<string>();
        }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 是否有任何错误（包括验证错误和全局错误）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private bool _hasValidationErrors;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否有效（无验证错误且无全局错误）
        /// </summary>
        public bool IsValid => !HasValidationErrors && !HasError;

        #endregion

        #region 构造函数

        protected ValidatingViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
            : base(loggerFactory, eventAggregator)
        {
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 设置属性验证错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="errors">错误消息列表</param>
        protected void SetErrors(string propertyName, IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            
            if (errorList.Count > 0)
            {
                _errors[propertyName] = errorList;
            }
            else if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
            }

            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// 添加单个验证错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="error">错误消息</param>
        protected void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out var errors))
            {
                errors = new List<string>();
                _errors[propertyName] = errors;
            }

            if (!errors.Contains(error))
            {
                errors.Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 清除指定属性的验证错误
        /// </summary>
        /// <param name="propertyName">属性名，null则清除所有</param>
        protected void ClearValidationErrors(string? propertyName = null)
        {
            if (propertyName == null)
            {
                var propertyNames = _errors.Keys.ToList();
                _errors.Clear();
                foreach (var name in propertyNames)
                {
                    OnErrorsChanged(name);
                }
            }
            else if (_errors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 触发错误变更事件
        /// </summary>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            HasValidationErrors = _errors.Any(e => e.Value.Count > 0);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(IsValid));
        }

        /// <summary>
        /// 获取属性的第一个错误消息
        /// </summary>
        protected string? GetFirstError(string propertyName)
        {
            return _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0
                ? errors[0]
                : null;
        }

        #endregion

        #region FluentValidation集成

        /// <summary>
        /// 使用FluentValidation验证对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="instance">要验证的对象</param>
        /// <param name="validator">验证器</param>
        /// <returns>验证是否通过</returns>
        protected async Task<bool> ValidateAsync<T>(T instance, IValidator<T> validator)
        {
            ClearValidationErrors();

            var result = await validator.ValidateAsync(instance);
            
            if (!result.IsValid)
            {
                ApplyValidationResult(result);
            }

            return result.IsValid;
        }

        /// <summary>
        /// 同步验证对象
        /// </summary>
        protected bool Validate<T>(T instance, IValidator<T> validator)
        {
            ClearValidationErrors();

            var result = validator.Validate(instance);
            
            if (!result.IsValid)
            {
                ApplyValidationResult(result);
            }

            return result.IsValid;
        }

        /// <summary>
        /// 验证单个属性
        /// </summary>
        protected async Task<bool> ValidatePropertyAsync<T>(
            T instance,
            IValidator<T> validator,
            string propertyName)
        {
            ClearValidationErrors(propertyName);

            var result = await validator.ValidateAsync(
                instance,
                options => options.IncludeProperties(propertyName));

            if (!result.IsValid)
            {
                var propertyErrors = result.Errors
                    .Where(e => e.PropertyName == propertyName)
                    .Select(e => e.ErrorMessage);
                SetErrors(propertyName, propertyErrors);
            }

            return !result.Errors.Any(e => e.PropertyName == propertyName);
        }

        /// <summary>
        /// 应用验证结果到错误字典
        /// </summary>
        private void ApplyValidationResult(ValidationResult result)
        {
            var errorsByProperty = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToList());

            foreach (var (propertyName, errors) in errorsByProperty)
            {
                _errors[propertyName] = errors;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }

            HasValidationErrors = result.Errors.Count > 0;
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(IsValid));
        }

        #endregion

        #region 属性设置辅助

        /// <summary>
        /// 设置属性并清除该属性的验证错误
        /// </summary>
        protected bool SetPropertyAndClearError<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            if (propertyName != null)
            {
                ClearValidationErrors(propertyName);
                OnPropertyChanged(propertyName);
            }
            return true;
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清除所有错误（包括验证错误和全局错误）
        /// </summary>
        protected void ClearAllErrors()
        {
            ClearValidationErrors();
            ClearError();
        }

        #endregion
    }
}
