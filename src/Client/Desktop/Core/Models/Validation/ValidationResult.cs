using LYBT.Shared.Models.Contracts.Common;
using System.Collections.Generic;
using System.Linq;

namespace LYBT.Desktop.Core.Models.Validation
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// 警告列表（不影响通过）
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 第一个错误消息
        /// </summary>
        public string? FirstError => Errors?.FirstOrDefault()?.Message;

        /// <summary>
        /// 所有错误消息
        /// </summary>
        public string AllErrors => string.Join("; ", Errors?.Select(e => e.Message) ?? Enumerable.Empty<string>());

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        public static ValidationResult Failure(string field, string message)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new ValidationError { Field = field, Message = message }
                }
            };
        }

        /// <summary>
        /// 创建失败的验证结果（简单消息）
        /// </summary>
        public static ValidationResult Failure(string message)
        {
            return Failure(string.Empty, message);
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError { Field = field, Message = message });
            IsValid = false;
        }

        /// <summary>
        /// 添加错误（简单消息）
        /// </summary>
        public ValidationResult AddError(string message)
        {
            AddError(string.Empty, message);
            return this;
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// 获取所有错误消息的字符串表示
        /// </summary>
        public string GetErrorsAsString(string separator = "; ")
        {
            return AllErrors;
        }

        /// <summary>
        /// 获取所有警告消息的字符串表示
        /// </summary>
        public string GetWarningsAsString(string separator = "; ")
        {
            return string.Join(separator, Warnings);
        }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误级别
        /// </summary>
        public ValidationErrorLevel Level { get; set; } = ValidationErrorLevel.Error;
    }

    /// <summary>
    /// 验证错误级别
    /// </summary>
    public enum ValidationErrorLevel
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 严重错误
        /// </summary>
        Critical
    }
}