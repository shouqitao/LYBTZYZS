using System;
using System.Text.RegularExpressions;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.UserAggregate.ValueObjects
{
    /// <summary>
    /// 用户名值对象 - UltraThink重构DDD架构
    /// 封装用户名的业务规则和验证逻辑
    /// </summary>
    public class UserName : SingleValueObject<string>
    {
        // 用户名验证正则表达式：3-50个字符，支持字母、数字、下划线、中文
        private static readonly Regex ValidUserNameRegex = new(@"^[\w\u4e00-\u9fa5]{3,50}$", RegexOptions.Compiled);

        private UserName(string value) : base(value)
        {
        }

        /// <summary>
        /// 创建用户名值对象
        /// </summary>
        /// <param name="value">用户名字符串</param>
        /// <returns>用户名值对象</returns>
        /// <exception cref="ArgumentException">用户名格式不正确时抛出</exception>
        public static UserName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("用户名不能为空", nameof(value));

            value = value.Trim();

            if (!IsValid(value))
                throw new ArgumentException($"用户名格式不正确。用户名必须是3-50个字符，支持字母、数字、下划线和中文字符。当前值: '{value}'", nameof(value));

            return new UserName(value);
        }

        /// <summary>
        /// 验证用户名格式
        /// </summary>
        /// <param name="value">用户名字符串</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && ValidUserNameRegex.IsMatch(value.Trim());
        }

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(UserName userName)
        {
            return userName?.Value;
        }
    }

    /// <summary>
    /// 真实姓名值对象
    /// </summary>
    public class RealName : SingleValueObject<string>
    {
        // 姓名验证：1-100个字符，支持字母、中文、常见标点
        private static readonly Regex ValidRealNameRegex = new(@"^[\u4e00-\u9fa5a-zA-Z\s\.\-]{1,100}$", RegexOptions.Compiled);

        private RealName(string value) : base(value)
        {
        }

        /// <summary>
        /// 创建真实姓名值对象
        /// </summary>
        /// <param name="value">姓名字符串</param>
        /// <returns>姓名值对象</returns>
        public static RealName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("真实姓名不能为空", nameof(value));

            value = value.Trim();

            if (!IsValid(value))
                throw new ArgumentException($"真实姓名格式不正确。姓名长度必须在1-100个字符之间，支持中文、英文字母、空格、点号和连字符。当前值: '{value}'", nameof(value));

            return new RealName(value);
        }

        /// <summary>
        /// 验证真实姓名格式
        /// </summary>
        /// <param name="value">姓名字符串</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && ValidRealNameRegex.IsMatch(value.Trim());
        }

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(RealName realName)
        {
            return realName?.Value;
        }
    }

    /// <summary>
    /// 邮箱地址值对象
    /// </summary>
    public class Email : SingleValueObject<string>
    {
        // 邮箱验证正则表达式
        private static readonly Regex ValidEmailRegex = new(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Email(string value) : base(value)
        {
        }

        /// <summary>
        /// 创建邮箱值对象
        /// </summary>
        /// <param name="value">邮箱字符串</param>
        /// <returns>邮箱值对象</returns>
        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("邮箱地址不能为空", nameof(value));

            value = value.Trim().ToLowerInvariant();

            if (!IsValid(value))
                throw new ArgumentException($"邮箱格式不正确。当前值: '{value}'", nameof(value));

            if (value.Length > 254) // RFC 5321 邮箱长度限制
                throw new ArgumentException("邮箱地址长度不能超过254个字符", nameof(value));

            return new Email(value);
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        /// <param name="value">邮箱字符串</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && ValidEmailRegex.IsMatch(value.Trim());
        }

        /// <summary>
        /// 获取邮箱域名
        /// </summary>
        public string Domain => Value.Split('@')[1];

        /// <summary>
        /// 获取邮箱用户名部分
        /// </summary>
        public string LocalPart => Value.Split('@')[0];

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(Email email)
        {
            return email?.Value;
        }
    }

    /// <summary>
    /// 电话号码值对象
    /// </summary>
    public class PhoneNumber : SingleValueObject<string>
    {
        // 电话号码验证：支持中国大陆手机号和固定电话
        private static readonly Regex ValidPhoneRegex = new(
            @"^(1[3-9]\d{9}|0\d{2,3}-?\d{7,8})$", 
            RegexOptions.Compiled);

        private PhoneNumber(string value) : base(value)
        {
        }

        /// <summary>
        /// 创建电话号码值对象
        /// </summary>
        /// <param name="value">电话号码字符串</param>
        /// <returns>电话号码值对象</returns>
        public static PhoneNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null; // 电话号码可以为空

            value = value.Trim().Replace(" ", "").Replace("-", "");

            if (!IsValid(value))
                throw new ArgumentException($"电话号码格式不正确。请输入有效的中国大陆手机号或固定电话。当前值: '{value}'", nameof(value));

            return new PhoneNumber(value);
        }

        /// <summary>
        /// 验证电话号码格式
        /// </summary>
        /// <param name="value">电话号码字符串</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().Replace(" ", "").Replace("-", "");
            return ValidPhoneRegex.IsMatch(value);
        }

        /// <summary>
        /// 判断是否为手机号
        /// </summary>
        public bool IsMobile => Value.StartsWith("1") && Value.Length == 11;

        /// <summary>
        /// 判断是否为固定电话
        /// </summary>
        public bool IsLandline => Value.StartsWith("0");

        /// <summary>
        /// 获取格式化的电话号码（手机号中间加空格）
        /// </summary>
        public string FormattedNumber
        {
            get
            {
                if (IsMobile)
                {
                    return $"{Value.Substring(0, 3)} {Value.Substring(3, 4)} {Value.Substring(7)}";
                }
                return Value;
            }
        }

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(PhoneNumber phoneNumber)
        {
            return phoneNumber?.Value;
        }
    }
}