using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Core.Models.Users
{
    /// <summary>
    /// 用户信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class UserInfo : BaseUserModel
    {
        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrEmpty(RealName) ? Username : RealName;

        /// <summary>状态文本</summary>
        public string StatusText => Status.GetDescription();

        /// <summary>是否为系统管理员（基于用户名判断）</summary>
        public bool IsSysAdmin => Username == "sysadmin";
    }
}