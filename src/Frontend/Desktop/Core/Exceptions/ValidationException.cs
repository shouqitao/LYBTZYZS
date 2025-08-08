using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.Exceptions
{
    /// <summary>
    /// 验证异常
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<ValidationError> ValidationErrors { get; }

        /// <summary>
        /// 用户友好消息
        /// </summary>
        public string UserMessage { get; }

        public ValidationException(string userMessage) : base(userMessage)
        {
            UserMessage = userMessage;
            ValidationErrors = new List<ValidationError>();
        }

        public ValidationException(string userMessage, List<ValidationError> validationErrors) : base(userMessage)
        {
            UserMessage = userMessage;
            ValidationErrors = validationErrors ?? new List<ValidationError>();
        }

        public ValidationException(string userMessage, string field, string error) : base(userMessage)
        {
            UserMessage = userMessage;
            ValidationErrors = new List<ValidationError>
            {
                new ValidationError(field, error)
            };
        }

        public ValidationException(string message, string userMessage, List<ValidationError> validationErrors) : base(message)
        {
            UserMessage = userMessage;
            ValidationErrors = validationErrors ?? new List<ValidationError>();
        }

        /// <summary>
        /// 添加验证错误
        /// </summary>
        public void AddError(string field, string error)
        {
            ValidationErrors.Add(new ValidationError(field, error));
        }

        /// <summary>
        /// 获取第一个错误消息
        /// </summary>
        public string GetFirstError()
        {
            return ValidationErrors.FirstOrDefault()?.Error ?? UserMessage;
        }

        /// <summary>
        /// 获取所有错误消息（格式化）
        /// </summary>
        public string GetFormattedErrors()
        {
            if (!ValidationErrors.Any())
                return UserMessage;

            var errors = ValidationErrors.Select(e => $"• {e.Field}: {e.Error}");
            return $"{UserMessage}\n{string.Join("\n", errors)}";
        }
    }

    /// <summary>
    /// 验证错误项
    /// </summary>
    public class ValidationError
    {
        public string Field { get; set; }
        public string Error { get; set; }

        public ValidationError(string field, string error)
        {
            Field = field ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }
}