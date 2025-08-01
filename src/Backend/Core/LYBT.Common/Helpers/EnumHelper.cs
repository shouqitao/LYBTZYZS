// 此文件已迁移到共享项目
// WPF特定功能已移至 Frontend/Desktop/Core/Helpers/WpfEnumHelper.cs
// 通用功能已移至 Shared/LYBT.Shared.Utilities/Helpers/EnumHelper.cs

// 为了保持向后兼容，此文件重新导出共享功能
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Common.Helpers
{
    /// <summary>
    /// 枚举工具类 - 向后兼容包装器
    /// 实际功能已迁移到 LYBT.Shared.Utilities.Helpers.EnumHelper
    /// </summary>
    [Obsolete("请直接使用 LYBT.Shared.Utilities.Helpers.EnumHelper")]
    public static class EnumHelper
    {
        /// <summary>
        /// 获取枚举值的显示名称
        /// </summary>
        public static string GetDescription<T>(T enumValue) where T : Enum
            => Shared.Utilities.Helpers.EnumHelper.GetDescription(enumValue);

        /// <summary>
        /// 获取枚举类型的所有值和描述
        /// </summary>
        public static Dictionary<T, string> GetEnumDescriptions<T>() where T : Enum
            => Shared.Utilities.Helpers.EnumHelper.GetEnumDescriptions<T>();

        /// <summary>
        /// 根据描述获取枚举值
        /// </summary>
        public static T GetEnumByDescription<T>(string description) where T : Enum
            => Shared.Utilities.Helpers.EnumHelper.GetEnumByDescription<T>(description);

        /// <summary>
        /// 获取枚举的键值对列表（用于下拉框等）
        /// </summary>
        public static List<KeyValuePair<T, string>> GetKeyValuePairs<T>() where T : Enum
            => Shared.Utilities.Helpers.EnumHelper.GetKeyValuePairs<T>();

        /// <summary>
        /// 获取枚举的整数值和描述的键值对列表
        /// </summary>
        public static List<KeyValuePair<int, string>> GetIntKeyValuePairs<T>() where T : Enum
            => Shared.Utilities.Helpers.EnumHelper.GetIntKeyValuePairs<T>();
    }
}