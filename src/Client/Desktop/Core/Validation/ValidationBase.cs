using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Validation
{
    /// <summary>
    /// 带FluentValidation支持的可观察对象基类
    /// </summary>
    public abstract class ValidationBase : Mvvm.ObservableObject, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _validationErrors = new();
        private readonly SemaphoreSlim _validationSemaphore = new(1, 1);
        private IValidator? _validator;

        /// <summary>
        /// 错误变更事件
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected ValidationBase()
        {
            InitializeValidator();
        }

        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasErrors => _validationErrors.Any();

        /// <summary>
        /// 是否已验证
        /// </summary>
        public bool IsValidated { get; private set; }

        /// <summary>
        /// 获取属性的错误信息
        /// </summary>
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                // 返回所有错误
                return _validationErrors.SelectMany(kvp => kvp.Value);
            }

            return _validationErrors.TryGetValue(propertyName, out var errors) 
                ? errors 
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// 获取属性的错误列表
        /// </summary>
        public IReadOnlyList<string> GetPropertyErrors(string propertyName)
        {
            return _validationErrors.TryGetValue(propertyName, out var errors)
                ? errors.AsReadOnly()
                : Array.Empty<string>();
        }

        /// <summary>
        /// 获取所有错误消息
        /// </summary>
        public IReadOnlyDictionary<string, List<string>> GetAllErrors()
        {
            return new Dictionary<string, List<string>>(_validationErrors);
        }

        /// <summary>
        /// 初始化验证器
        /// </summary>
        protected virtual void InitializeValidator()
        {
            _validator = CreateValidator();
        }

        /// <summary>
        /// 创建验证器（子类重写）
        /// </summary>
        protected abstract IValidator? CreateValidator();

        /// <summary>
        /// 设置自定义验证器
        /// </summary>
        protected void SetValidator(IValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// 设置属性值（带自动验证）
        /// </summary>
        protected override bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            var result = base.SetProperty(ref field, value, propertyName);
            
            if (result && propertyName != null)
            {
                // 异步验证属性
                _ = ValidatePropertyAsync(propertyName, value);
            }
            
            return result;
        }

        /// <summary>
        /// 验证单个属性
        /// </summary>
        protected virtual async Task<bool> ValidatePropertyAsync(string propertyName, object? value)
        {
            await _validationSemaphore.WaitAsync();
            try
            {
                // 清除旧错误
                ClearPropertyErrors(propertyName);

                if (_validator == null)
                    return true;

                // 使用FluentValidation验证
                var context = new ValidationContext<object>(this, new PropertyChain(), 
                    new MemberNameValidatorSelector(new[] { propertyName }));
                
                var validationResult = await _validator.ValidateAsync(context);
                
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .Where(e => e.PropertyName == propertyName)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    if (errors.Any())
                    {
                        AddPropertyErrors(propertyName, errors);
                        return false;
                    }
                }
                
                return true;
            }
            finally
            {
                _validationSemaphore.Release();
            }
        }

        /// <summary>
        /// 验证所有属性
        /// </summary>
        public virtual async Task<bool> ValidateAsync()
        {
            await _validationSemaphore.WaitAsync();
            try
            {
                ClearAllErrors();

                if (_validator == null)
                {
                    IsValidated = true;
                    return true;
                }

                var context = new ValidationContext<ValidationBase>(this);
                var validationResult = await _validator.ValidateAsync(context);
                
                if (!validationResult.IsValid)
                {
                    var errorGroups = validationResult.Errors.GroupBy(e => e.PropertyName);
                    
                    foreach (var group in errorGroups)
                    {
                        var errors = group.Select(e => e.ErrorMessage).ToList();
                        AddPropertyErrors(group.Key, errors);
                    }
                }
                
                IsValidated = true;
                return !HasErrors;
            }
            finally
            {
                _validationSemaphore.Release();
            }
        }

        /// <summary>
        /// 同步验证
        /// </summary>
        public virtual bool Validate()
        {
            return ValidateAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 获取验证结果
        /// </summary>
        public async Task<ValidationResult> GetValidationResultAsync()
        {
            if (_validator == null)
                return new ValidationResult();
            
            var context = new ValidationContext<ValidationBase>(this);
            return await _validator.ValidateAsync(context);
        }

        /// <summary>
        /// 添加自定义错误
        /// </summary>
        protected void AddError(string propertyName, string errorMessage)
        {
            AddPropertyErrors(propertyName, new[] { errorMessage });
        }

        /// <summary>
        /// 添加属性错误
        /// </summary>
        private void AddPropertyErrors(string propertyName, IEnumerable<string> errors)
        {
            if (!_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors[propertyName] = new List<string>();
            }
            
            _validationErrors[propertyName].AddRange(errors);
            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// 清除属性错误
        /// </summary>
        protected void ClearPropertyErrors(string propertyName)
        {
            if (_validationErrors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 清除所有错误
        /// </summary>
        protected void ClearAllErrors()
        {
            var properties = _validationErrors.Keys.ToArray();
            _validationErrors.Clear();
            
            foreach (var propertyName in properties)
            {
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 触发错误变更事件
        /// </summary>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// 获取第一个错误消息
        /// </summary>
        public string? GetFirstError()
        {
            return _validationErrors.Values.SelectMany(v => v).FirstOrDefault();
        }

        /// <summary>
        /// 获取格式化的错误消息
        /// </summary>
        public string GetFormattedErrors(string separator = "\n")
        {
            var errors = _validationErrors.SelectMany(kvp => 
                kvp.Value.Select(e => $"{kvp.Key}: {e}"));
            return string.Join(separator, errors);
        }
    }

    /// <summary>
    /// 泛型验证基类
    /// </summary>
    public abstract class ValidationBase<T> : ValidationBase where T : ValidationBase<T>
    {
        private IValidator<T>? _typedValidator;

        /// <summary>
        /// 初始化验证器
        /// </summary>
        protected override void InitializeValidator()
        {
            _typedValidator = CreateTypedValidator();
            if (_typedValidator != null)
            {
                SetValidator(_typedValidator);
            }
        }

        /// <summary>
        /// 创建类型化验证器
        /// </summary>
        protected abstract IValidator<T>? CreateTypedValidator();

        /// <summary>
        /// 创建验证器
        /// </summary>
        protected override IValidator? CreateValidator()
        {
            return _typedValidator;
        }
    }

    /// <summary>
    /// 成员名称验证器选择器
    /// </summary>
    internal class MemberNameValidatorSelector : IValidatorSelector
    {
        private readonly HashSet<string> _memberNames;

        public MemberNameValidatorSelector(IEnumerable<string> memberNames)
        {
            _memberNames = new HashSet<string>(memberNames);
        }

        public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context)
        {
            return _memberNames.Contains(propertyPath);
        }
    }
}