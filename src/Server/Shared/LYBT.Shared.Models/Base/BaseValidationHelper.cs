using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Base
{
    /// <summary>
    /// 通用验证助手基类 - UltraThink Helper重构
    /// 抽取各模块ValidationHelper中的通用验证逻辑
    /// </summary>
    public abstract class BaseValidationHelper
    {
        #region 基础字段验证

        /// <summary>
        /// 验证必填字符串字段
        /// </summary>
        /// <param name="value">待验证值</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateRequiredString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能为空");
            }
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证字符串长度
        /// </summary>
        /// <param name="value">待验证值</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="maxLength">最大长度</param>
        /// <param name="minLength">最小长度（默认0）</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateStringLength(string value, string fieldName, int maxLength, int minLength = 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ServiceResult<bool>.Success(true); // 长度验证不检查null，由Required验证处理
            }

            if (value.Length < minLength)
            {
                return ServiceResult<bool>.Failure($"{fieldName}长度不能少于{minLength}个字符");
            }

            if (value.Length > maxLength)
            {
                return ServiceResult<bool>.Failure($"{fieldName}长度不能超过{maxLength}个字符");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证GUID是否有效
        /// </summary>
        /// <param name="id">待验证ID</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateGuid(Guid id, string fieldName)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能为空");
            }
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证可空GUID
        /// </summary>
        /// <param name="id">待验证ID</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="required">是否必填</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateNullableGuid(Guid? id, string fieldName, bool required = false)
        {
            if (required && (!id.HasValue || id.Value == Guid.Empty))
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能为空");
            }
            
            if (id.HasValue && id.Value == Guid.Empty)
            {
                return ServiceResult<bool>.Failure($"{fieldName}格式无效");
            }
            
            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 数值验证

        /// <summary>
        /// 验证数值范围
        /// </summary>
        /// <param name="value">待验证值</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateNumericRange(decimal value, string fieldName, decimal min, decimal max)
        {
            if (value < min || value > max)
            {
                return ServiceResult<bool>.Failure($"{fieldName}必须在{min}和{max}之间");
            }
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证正数
        /// </summary>
        /// <param name="value">待验证值</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="allowZero">是否允许零</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidatePositiveNumber(decimal value, string fieldName, bool allowZero = false)
        {
            if (allowZero && value < 0)
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能为负数");
            }
            
            if (!allowZero && value <= 0)
            {
                return ServiceResult<bool>.Failure($"{fieldName}必须大于0");
            }
            
            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 格式验证

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        /// <param name="email">待验证邮箱</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateEmail(string email, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<bool>.Success(true); // 空值由Required验证处理
            }

            try
            {
                var emailAttribute = new EmailAddressAttribute();
                if (!emailAttribute.IsValid(email))
                {
                    return ServiceResult<bool>.Failure($"{fieldName}格式不正确");
                }
                return ServiceResult<bool>.Success(true);
            }
            catch
            {
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");
            }
        }

        /// <summary>
        /// 验证手机号格式
        /// </summary>
        /// <param name="phone">待验证手机号</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidatePhoneNumber(string phone, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return ServiceResult<bool>.Success(true); // 空值由Required验证处理
            }

            // 中国手机号正则：1开头，第二位3-9，总共11位数字
            var phoneRegex = new Regex(@"^1[3-9]\d{9}$");
            if (!phoneRegex.IsMatch(phone))
            {
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证身份证号格式
        /// </summary>
        /// <param name="idCard">待验证身份证号</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateIdCard(string idCard, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return ServiceResult<bool>.Success(true); // 空值由Required验证处理
            }

            // 简单的身份证号格式验证：18位数字或17位数字+X
            var idCardRegex = new Regex(@"^(\d{17}[\dXx]|\d{15})$");
            if (!idCardRegex.IsMatch(idCard))
            {
                return ServiceResult<bool>.Failure($"{fieldName}格式不正确");
            }

            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 业务规则验证

        /// <summary>
        /// 验证日期范围
        /// </summary>
        /// <param name="date">待验证日期</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="minDate">最小日期</param>
        /// <param name="maxDate">最大日期</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateDateRange(DateTime date, string fieldName, DateTime? minDate = null, DateTime? maxDate = null)
        {
            if (minDate.HasValue && date < minDate.Value)
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能早于{minDate.Value:yyyy-MM-dd}");
            }

            if (maxDate.HasValue && date > maxDate.Value)
            {
                return ServiceResult<bool>.Failure($"{fieldName}不能晚于{maxDate.Value:yyyy-MM-dd}");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证年龄范围
        /// </summary>
        /// <param name="birthDate">出生日期</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="minAge">最小年龄</param>
        /// <param name="maxAge">最大年龄</param>
        /// <returns>验证结果</returns>
        protected static ServiceResult<bool> ValidateAge(DateTime birthDate, string fieldName, int minAge = 0, int maxAge = 150)
        {
            var age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age < minAge)
            {
                return ServiceResult<bool>.Failure($"年龄不能小于{minAge}岁");
            }

            if (age > maxAge)
            {
                return ServiceResult<bool>.Failure($"年龄不能大于{maxAge}岁");
            }

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}