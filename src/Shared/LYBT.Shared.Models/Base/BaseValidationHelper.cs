using System;
using System.Text.RegularExpressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Base
{
    /// <summary>
    /// 验证助手基类 - UltraThink Helper模式
    /// 提供通用的验证方法，减少Helper类间的代码重复
    /// </summary>
    public abstract class BaseValidationHelper
    {
        /// <summary>
        /// 验证必填字符串
        /// </summary>
        protected static ServiceResult<bool> ValidateRequiredString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ServiceResult<bool>.Failure($"{fieldName}不能为空");
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证字符串长度
        /// </summary>
        protected static ServiceResult<bool> ValidateStringLength(string value, string fieldName, int maxLength, int minLength = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ServiceResult<bool>.Success(true); // 空值跳过长度验证

            if (value.Length < minLength)
                return ServiceResult<bool>.Failure($"{fieldName}不能少于{minLength}个字符");

            if (value.Length > maxLength)
                return ServiceResult<bool>.Failure($"{fieldName}不能超过{maxLength}个字符");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证GUID
        /// </summary>
        protected static ServiceResult<bool> ValidateGuid(Guid id, string fieldName)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure($"{fieldName}不能为空");
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证数值范围
        /// </summary>
        protected static ServiceResult<bool> ValidateNumericRange(decimal value, string fieldName, decimal min, decimal max)
        {
            if (value < min || value > max)
                return ServiceResult<bool>.Failure($"{fieldName}必须在{min}和{max}之间");
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证正数
        /// </summary>
        protected static ServiceResult<bool> ValidatePositiveNumber(decimal value, string fieldName, bool allowZero = false)
        {
            if (value < 0)
                return ServiceResult<bool>.Failure($"{fieldName}不能为负数");

            if (!allowZero && value == 0)
                return ServiceResult<bool>.Failure($"{fieldName}必须大于0");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证手机号码格式（中国手机号）
        /// </summary>
        protected static ServiceResult<bool> ValidatePhoneNumber(string phone, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return ServiceResult<bool>.Success(true); // 手机号是可选的

            // 中国手机号正则：1开头，第二位是3-9，总共11位数字
            var phoneRegex = new Regex(@"^1[3-9]\d{9}$");
            if (!phoneRegex.IsMatch(phone))
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        protected static ServiceResult<bool> ValidateEmail(string email, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<bool>.Success(true); // 邮箱是可选的

            // 简化的邮箱正则验证
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailRegex.IsMatch(email))
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证身份证号码格式（中国身份证）
        /// </summary>
        protected static ServiceResult<bool> ValidateIdCard(string idCard, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(idCard))
                return ServiceResult<bool>.Success(true); // 身份证是可选的

            // 中国身份证正则：15位或18位，18位最后一位可以是X
            var idCardRegex = new Regex(@"^(\d{15}|\d{17}[\dX])$", RegexOptions.IgnoreCase);
            if (!idCardRegex.IsMatch(idCard))
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");

            return ServiceResult<bool>.Success(true);
        }
    }
}