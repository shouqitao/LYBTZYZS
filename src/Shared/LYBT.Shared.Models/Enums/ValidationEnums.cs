using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 业务操作类型枚举
    /// OpenSpec: unify-enums-to-shared - 从ValidationContext.cs迁移
    /// </summary>
    public enum BusinessOperation
    {
        /// <summary>创建操作</summary>
        [Description("创建")]
        Create = 0,

        /// <summary>更新操作</summary>
        [Description("更新")]
        Update = 1,

        /// <summary>删除操作</summary>
        [Description("删除")]
        Delete = 2,

        /// <summary>查询操作</summary>
        [Description("查询")]
        Read = 3,

        /// <summary>状态切换操作</summary>
        [Description("状态切换")]
        ToggleStatus = 4,

        /// <summary>自定义操作</summary>
        [Description("自定义")]
        Custom = 99
    }
}
