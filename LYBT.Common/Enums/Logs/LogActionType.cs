using System.ComponentModel;

namespace LYBT.Common.Enums.Logs {

    /// <summary>
    /// 操作类型枚举（用于系统日志记录）
    /// </summary>
    [Description("日志操作类型")]
    public enum LogActionType {

        [Description("新增")]
        Create = 0,

        [Description("修改")]
        Update = 1,

        [Description("删除")]
        Delete = 2,

        [Description("查询")]
        Query = 3,

        [Description("登录")]
        Login = 4,

        [Description("登出")]
        Logout = 5,

        [Description("同步")]
        Sync = 6
    }
}