using System;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;

namespace LYBT.WPF.Client.Core.Models.Users {
    /// <summary>
    /// 用户信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class UserInfo : BaseUserModel {
        /// <summary>是否有超级管理员权限</summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>头像URL</summary>
        public string? Avatar { get; set; }

        /// <summary>是否在线</summary>
        public bool IsOnline { get; set; }

        /// <summary>最后登录IP</summary>
        public string? LastLoginIp { get; set; }
    }
}