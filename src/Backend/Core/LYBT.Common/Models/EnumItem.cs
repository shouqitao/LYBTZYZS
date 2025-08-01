// 此类已迁移到共享项目
// 实际功能已移至 LYBT.Shared.Models.Common.EnumItem<T>

using LYBT.Shared.Models.Common;

namespace LYBT.Common.Models
{
    /// <summary>
    /// 枚举项模型 - 向后兼容包装器
    /// 实际功能已迁移到 LYBT.Shared.Models.Common.EnumItem
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    [Obsolete("请直接使用 LYBT.Shared.Models.Common.EnumItem<T>")]
    public class EnumItem<T> : LYBT.Shared.Models.Common.EnumItem<T> where T : Enum
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EnumItem() : base() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <param name="text">显示文本</param>
        public EnumItem(T value, string text) : base(value, text) { }
    }
}