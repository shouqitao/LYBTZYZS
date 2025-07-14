using System.ComponentModel;

namespace LYBT.Common.Enums {
    /// <summary>
    /// Profile view mode
    /// </summary>
    public enum ProfileMode {
        [Description("查看")]
        View = 0,
        [Description("编辑")]
        Edit = 1,
        [Description("新增")]
        Create = 2
    }
}
