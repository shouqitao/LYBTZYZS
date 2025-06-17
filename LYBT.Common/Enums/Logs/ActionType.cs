using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.Common.Enums.Logs {
    /// <summary>
    /// 操作类型枚举
    /// </summary>
    public enum ActionType {
        /// <summary>
        /// 新增
        /// </summary>
        [Description("新增")]
        Create = 1,

        /// <summary>
        /// 编辑
        /// </summary>
        [Description("编辑")]
        Edit = 2,

        /// <summary>
        /// 禁用
        /// </summary>
        [Description("禁用")]
        Disable = 3,

        /// <summary>
        /// 启用
        /// </summary>
        [Description("启用")]
        Enable = 4,

        /// <summary>
        /// 重置密码
        /// </summary>
        [Description("重置密码")]
        ResetPassword = 5,

        /// <summary>
        /// 登录
        /// </summary>
        [Description("登录")]
        Login = 6,

        /// <summary>
        /// 登出
        /// </summary>
        [Description("登出")]
        Logout = 7,

        /// <summary>
        /// 其他
        /// </summary>
        [Description("其他")]
        Other = 99
    }
}