using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 可验证模型基类 - 为DetailModel提供验证支持
    /// OpenSpec: ui-validation-framework
    /// 
    /// 提供INotifyDataErrorInfo实现和DataAnnotations验证支持
    /// </summary>
    public abstract class ValidatableModelBase : BindableBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _validationErrors = new();

        /// <summary>验证错误变更事件</summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>是否有验证错误</summary>
        public bool HasErrors => _validationErrors.Count > 0;

        /// <summary>获取指定属性的验证错误</summary>
        public IEnumerable GetErrors(string? propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? _validationErrors.SelectMany(x => x.Value)
                : _validationErrors.TryGetValue(propertyName, out var errors)
                    ? errors
                    : Enumerable.Empty<string>();

        /// <summary>验证错误访问器 - 支持XAML索引器绑定 Errors[PropertyName]</summary>
        public ValidationErrorsAccessor Errors { get; }

        /// <summary>属性错误状态访问器 - 支持XAML索引器绑定 HasErrorsDictionary[PropertyName]</summary>
        public ValidationHasErrorsAccessor HasErrorsDictionary { get; }

        /// <summary>构造函数</summary>
        protected ValidatableModelBase()
        {
            Errors = new ValidationErrorsAccessor(_validationErrors);
            HasErrorsDictionary = new ValidationHasErrorsAccessor(_validationErrors);
        }

        /// <summary>
        /// 设置属性并验证
        /// 属性值变更后自动执行DataAnnotations验证
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">后备字段</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名（自动获取）</param>
        /// <returns>是否成功设置</returns>
        protected bool SetPropertyAndValidate<T>(ref T storage, T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (!SetProperty(ref storage, value, propertyName))
                return false;

            ValidateProperty(propertyName);
            return true;
        }

        /// <summary>
        /// 验证指定属性
        /// 使用DataAnnotations验证属性值
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void ValidateProperty([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            ClearValidationErrors(propertyName);

            var property = GetType().GetProperty(propertyName);
            if (property == null)
                return;

            var value = property.GetValue(this);
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(this) { MemberName = propertyName };

            if (!Validator.TryValidateProperty(value, context, validationResults))
            {
                foreach (var result in validationResults)
                {
                    AddValidationError(propertyName, result.ErrorMessage ?? "验证失败");
                }
            }
        }

        /// <summary>
        /// 验证所有属性
        /// 遍历所有带有ValidationAttribute的属性并验证
        /// </summary>
        /// <returns>验证是否全部通过</returns>
        public virtual bool ValidateAll()
        {
            // 清除所有错误
            var propertyNames = _validationErrors.Keys.ToList();
            _validationErrors.Clear();
            foreach (var name in propertyNames)
            {
                OnErrorsChanged(name);
            }

            // 获取所有带验证特性的属性
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(ValidationAttribute), true).Length > 0);

            foreach (var property in properties)
            {
                ValidateProperty(property.Name);
            }

            RaisePropertyChanged(nameof(HasErrors));
            return !HasErrors;
        }

        /// <summary>添加验证错误</summary>
        protected void AddValidationError(string propertyName, string errorMessage)
        {
            if (!_validationErrors.TryGetValue(propertyName, out var errors))
            {
                errors = new List<string>();
                _validationErrors[propertyName] = errors;
            }

            if (!errors.Contains(errorMessage))
            {
                errors.Add(errorMessage);
                OnErrorsChanged(propertyName);
                RaisePropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>清除验证错误</summary>
        protected void ClearValidationErrors(string? propertyName = null)
        {
            if (propertyName == null)
            {
                var names = _validationErrors.Keys.ToList();
                _validationErrors.Clear();
                foreach (var name in names)
                {
                    OnErrorsChanged(name);
                }
            }
            else if (_validationErrors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }

            RaisePropertyChanged(nameof(HasErrors));
        }

        /// <summary>触发验证错误变更事件</summary>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
