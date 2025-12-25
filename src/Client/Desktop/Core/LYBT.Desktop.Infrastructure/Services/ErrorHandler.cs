using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 错误处理服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    public partial class ErrorHandler : ObservableObject, IErrorHandler
    {
        private readonly ILogger<ErrorHandler>? _logger;
        private readonly Dictionary<string, List<string>> _errors = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrors))]
        private string? _errorMessage;

        public ErrorHandler(ILogger<ErrorHandler>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool HasErrors => _errors.Count > 0 || !string.IsNullOrEmpty(ErrorMessage);

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> AllErrors =>
            _errors.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());

        /// <inheritdoc/>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <inheritdoc/>
        public event EventHandler<ErrorChangedEventArgs>? ErrorChanged;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void HandleException(Exception exception, string? context = null)
        {
            var message = context != null
                ? $"{context}: {exception.Message}"
                : exception.Message;

            ErrorMessage = message;
            _logger?.LogError(exception, "Error occurred: {Context}", context);

            ErrorChanged?.Invoke(this, new ErrorChangedEventArgs(null, new[] { message }));
        }

        /// <inheritdoc/>
        public void SetError(string propertyName, string error)
        {
            SetErrors(propertyName, new[] { error });
        }

        /// <inheritdoc/>
        public void SetErrors(string propertyName, IEnumerable<string> errors)
        {
            var errorList = errors.ToList();

            if (errorList.Count == 0)
            {
                ClearError(propertyName);
                return;
            }

            _errors[propertyName] = errorList;
            OnPropertyChanged(nameof(HasErrors));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            ErrorChanged?.Invoke(this, new ErrorChangedEventArgs(propertyName, errorList.AsReadOnly()));
        }

        /// <inheritdoc/>
        public void ClearError(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                OnPropertyChanged(nameof(HasErrors));
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                ErrorChanged?.Invoke(this, new ErrorChangedEventArgs(propertyName, Array.Empty<string>()));
            }
        }

        /// <inheritdoc/>
        public void ClearAllErrors()
        {
            var propertyNames = _errors.Keys.ToList();
            _errors.Clear();
            ErrorMessage = null;

            OnPropertyChanged(nameof(HasErrors));

            foreach (var propertyName in propertyNames)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                ErrorChanged?.Invoke(this, new ErrorChangedEventArgs(propertyName, Array.Empty<string>()));
            }
        }

        /// <inheritdoc/>
        public bool ValidateProperty(object? value, string propertyName)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(this) { MemberName = propertyName };

            var isValid = Validator.TryValidateProperty(value, context, results);

            if (isValid)
            {
                ClearError(propertyName);
            }
            else
            {
                SetErrors(propertyName, results.Select(r => r.ErrorMessage ?? "验证失败"));
            }

            return isValid;
        }

        /// <inheritdoc/>
        public bool ValidateAll(object target)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(target);

            var isValid = Validator.TryValidateObject(target, context, results, true);

            ClearAllErrors();

            if (!isValid)
            {
                foreach (var result in results)
                {
                    foreach (var memberName in result.MemberNames)
                    {
                        SetError(memberName, result.ErrorMessage ?? "验证失败");
                    }
                }
            }

            return isValid;
        }
    }
}
