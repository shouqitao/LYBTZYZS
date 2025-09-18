using System;
using System.Collections.Generic;
using Bogus;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Tests.Base
{
    /// <summary>
    /// 认证模块简化测试数据生成器 - UltraThink双层架构适配
    /// </summary>
    public static class AuthTestDataGenerator
    {
        /// <summary>
        /// 创建登录请求
        /// </summary>
        public static LoginRequest CreateLoginRequest(
            string username = "testuser",
            string password = "testpass123")
        {
            return new LoginRequest
            {
                Username = username,
                Password = password
            };
        }

        /// <summary>
        /// 创建登出请求
        /// </summary>
        public static LogoutRequest CreateLogoutRequest(string username = "testuser")
        {
            return new LogoutRequest
            {
                Username = username
            };
        }

        /// <summary>
        /// 创建系统管理员密码修改请求
        /// </summary>
        public static ChangeSysAdminPassword CreateChangePasswordRequest(string newPassword = "NewPass123")
        {
            return new ChangeSysAdminPassword
            {
                NewPassword = newPassword
            };
        }

        /// <summary>
        /// 创建登录响应
        /// </summary>
        public static LoginResponse CreateLoginResponse(
            string token = "test-jwt-token",
            string username = "testuser",
            string realName = "测试用户")
        {
            return new LoginResponse
            {
                Token = token,
                User = new UserDto
                {
                    Username = username,
                    RealName = realName,
                    Role = "Doctor",
                    Status = CommonStatus.Enabled
                },
                RefreshToken = "test-refresh-token",
                ExpiresAt = DateTime.Now.AddHours(8)
            };
        }

    }
}