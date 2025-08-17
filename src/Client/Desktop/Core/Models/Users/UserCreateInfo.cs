using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Users
{
    /// <summary>
    /// 创建用户信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的创建数据模型
    /// </summary>
    public class UserCreateInfo
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3到50个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// 真实姓名
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        public string RealName { get; set; } = string.Empty;
        
        /// <summary>
        /// 电话号码
        /// </summary>
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// 电子邮箱
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电子邮箱")]
        [StringLength(100, ErrorMessage = "电子邮箱长度不能超过100个字符")]
        public string? Email { get; set; }
        
        /// <summary>
        /// 角色
        /// </summary>
        [Required(ErrorMessage = "请选择用户角色")]
        public UserRole Role { get; set; } = UserRole.User;
        
        /// <summary>
        /// 部门
        /// </summary>
        [StringLength(50, ErrorMessage = "部门名称长度不能超过50个字符")]
        public string? Department { get; set; }
        
        /// <summary>
        /// 职位
        /// </summary>
        [StringLength(50, ErrorMessage = "职位名称长度不能超过50个字符")]
        public string? Position { get; set; }
        
        /// <summary>
        /// 拼音码
        /// </summary>
        [StringLength(100, ErrorMessage = "拼音码长度不能超过100个字符")]
        public string? PinYinCode { get; set; }
        
        /// <summary>
        /// 五笔码
        /// </summary>
        [StringLength(100, ErrorMessage = "五笔码长度不能超过100个字符")]
        public string? WuBiCode { get; set; }
        
        /// <summary>
        /// 初始密码（可选，不填则使用系统默认）
        /// </summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6到100个字符之间")]
        public string? InitialPassword { get; set; }
        
        /// <summary>
        /// 确认密码
        /// </summary>
        public string? ConfirmPassword { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
        
        /// <summary>
        /// 是否立即激活
        /// </summary>
        public bool IsActiveImmediately { get; set; } = true;
        
        #region UI状态属性
        
        /// <summary>
        /// 是否正在提交
        /// </summary>
        public bool IsSubmitting { get; set; }
        
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// 可用角色列表
        /// </summary>
        public List<UserRole> AvailableRoles { get; set; } = new();
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool IsValid()
        {
            ValidationErrors.Clear();
            
            if (string.IsNullOrWhiteSpace(Username))
            {
                ValidationErrors[nameof(Username)] = "用户名不能为空";
            }
            else if (Username.Length < 3 || Username.Length > 50)
            {
                ValidationErrors[nameof(Username)] = "用户名长度必须在3到50个字符之间";
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Username, @"^[a-zA-Z0-9_]+$"))
            {
                ValidationErrors[nameof(Username)] = "用户名只能包含字母、数字和下划线";
            }
            
            if (string.IsNullOrWhiteSpace(RealName))
            {
                ValidationErrors[nameof(RealName)] = "真实姓名不能为空";
            }
            else if (RealName.Length > 50)
            {
                ValidationErrors[nameof(RealName)] = "真实姓名长度不能超过50个字符";
            }
            
            if (!string.IsNullOrEmpty(PhoneNumber) && PhoneNumber.Length > 20)
            {
                ValidationErrors[nameof(PhoneNumber)] = "电话号码长度不能超过20个字符";
            }
            
            if (!string.IsNullOrEmpty(Email))
            {
                if (Email.Length > 100)
                {
                    ValidationErrors[nameof(Email)] = "电子邮箱长度不能超过100个字符";
                }
                else if (!IsValidEmail(Email))
                {
                    ValidationErrors[nameof(Email)] = "请输入有效的电子邮箱";
                }
            }
            
            if (!string.IsNullOrEmpty(InitialPassword))
            {
                if (InitialPassword.Length < 6 || InitialPassword.Length > 100)
                {
                    ValidationErrors[nameof(InitialPassword)] = "密码长度必须在6到100个字符之间";
                }
                
                if (InitialPassword != ConfirmPassword)
                {
                    ValidationErrors[nameof(ConfirmPassword)] = "确认密码与初始密码不匹配";
                }
            }
            
            if (!string.IsNullOrEmpty(Department) && Department.Length > 50)
            {
                ValidationErrors[nameof(Department)] = "部门名称长度不能超过50个字符";
            }
            
            if (!string.IsNullOrEmpty(Position) && Position.Length > 50)
            {
                ValidationErrors[nameof(Position)] = "职位名称长度不能超过50个字符";
            }
            
            if (!string.IsNullOrEmpty(Remark) && Remark.Length > 500)
            {
                ValidationErrors[nameof(Remark)] = "备注长度不能超过500个字符";
            }
            
            return ValidationErrors.Count == 0;
        }
        
        /// <summary>
        /// 自动生成拼音码和五笔码
        /// </summary>
        public void GenerateCodes()
        {
            if (!string.IsNullOrEmpty(RealName))
            {
                // 简单的拼音码生成（实际应该使用拼音库）
                PinYinCode = GeneratePinYinCode(RealName);
                
                // 简单的五笔码生成（实际应该使用五笔编码库）
                WuBiCode = GenerateWuBiCode(RealName);
            }
        }
        
        /// <summary>
        /// 重置表单
        /// </summary>
        public void Reset()
        {
            Username = string.Empty;
            RealName = string.Empty;
            PhoneNumber = null;
            Email = null;
            Role = UserRole.User;
            Department = null;
            Position = null;
            PinYinCode = null;
            WuBiCode = null;
            InitialPassword = null;
            ConfirmPassword = null;
            Remark = null;
            IsActiveImmediately = true;
            IsSubmitting = false;
            ValidationErrors.Clear();
        }
        
        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 生成拼音码（简化版）
        /// </summary>
        private string GeneratePinYinCode(string name)
        {
            // 这里应该使用专业的拼音库，暂时返回首字母
            if (string.IsNullOrEmpty(name))
                return string.Empty;
                
            return name.Substring(0, Math.Min(name.Length, 10)).ToUpper();
        }
        
        /// <summary>
        /// 生成五笔码（简化版）
        /// </summary>
        private string GenerateWuBiCode(string name)
        {
            // 这里应该使用专业的五笔编码库，暂时返回简化版
            if (string.IsNullOrEmpty(name))
                return string.Empty;
                
            return name.Substring(0, Math.Min(name.Length, 10)).ToUpper();
        }
        
        #endregion
    }
}