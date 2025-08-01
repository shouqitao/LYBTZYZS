using LYBT.Shared.Models.Extensions;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Utilities.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Client.Core.Helpers
{
    /// <summary>
    /// WPF专用枚举工具类
    /// 提供WPF数据绑定相关的枚举操作方法
    /// </summary>
    [Description("WPF枚举工具类")]
    public static class WpfEnumHelper
    {
        /// <summary>
        /// 构建适用于ComboBox绑定的ObservableCollection
        /// 每个项目包含枚举值和其描述文本
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>可观察集合</returns>
        public static ObservableCollection<EnumItem<T>> BuildComboBoxSource<T>() where T : Enum
        {
            var list = new ObservableCollection<EnumItem<T>>();
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                list.Add(new EnumItem<T>(value, value.GetDescription()));
            }
            return list;
        }

        /// <summary>
        /// 构建带有空选项的ComboBox数据源
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="emptyText">空选项显示文本</param>
        /// <returns>可观察集合</returns>
        public static ObservableCollection<NullableEnumItem<T>> BuildComboBoxSourceWithEmpty<T>(string emptyText = "请选择...") where T : struct, Enum
        {
            var list = new ObservableCollection<NullableEnumItem<T>>();
            
            // 添加空选项
            list.Add(new NullableEnumItem<T>(null, emptyText));
            
            // 添加枚举选项
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                list.Add(new NullableEnumItem<T>(value, value.GetDescription()));
            }
            
            return list;
        }

        /// <summary>
        /// 构建按分组的ComboBox数据源
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="groupSelector">分组选择器</param>
        /// <returns>分组的枚举项</returns>
        public static IEnumerable<IGrouping<string, EnumItem<T>>> BuildGroupedComboBoxSource<T>(
            Func<T, string> groupSelector) where T : Enum
        {
            var items = new List<EnumItem<T>>();
            
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                items.Add(new EnumItem<T>(value, value.GetDescription()));
            }
            
            return items.GroupBy(item => groupSelector(item.Value));
        }

        /// <summary>
        /// 从ComboBox选中项获取枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="selectedItem">选中项</param>
        /// <returns>枚举值，如果未选中则返回默认值</returns>
        public static T? GetSelectedEnumValue<T>(EnumItem<T>? selectedItem) where T : Enum
        {
            return selectedItem?.Value;
        }

        /// <summary>
        /// 在ComboBox数据源中查找指定枚举值的项
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="value">要查找的枚举值</param>
        /// <returns>匹配的项，如果未找到则返回null</returns>
        public static EnumItem<T>? FindEnumItem<T>(ObservableCollection<EnumItem<T>> source, T value) where T : Enum
        {
            return source.FirstOrDefault(item => Equals(item.Value, value));
        }

        /// <summary>
        /// 设置ComboBox的选中项
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="value">要选中的枚举值</param>
        /// <returns>选中的项，如果未找到则返回null</returns>
        public static EnumItem<T>? SetSelectedValue<T>(ObservableCollection<EnumItem<T>> source, T value) where T : Enum
        {
            return FindEnumItem(source, value);
        }

        /// <summary>
        /// 继承共享EnumHelper的功能
        /// </summary>
        public static class Shared
        {
            public static string GetDescription<T>(T enumValue) where T : Enum
                => EnumHelper.GetDescription(enumValue);

            public static Dictionary<T, string> GetEnumDescriptions<T>() where T : Enum
                => EnumHelper.GetEnumDescriptions<T>();

            public static T GetEnumByDescription<T>(string description) where T : Enum
                => EnumHelper.GetEnumByDescription<T>(description);

            public static List<KeyValuePair<T, string>> GetKeyValuePairs<T>() where T : Enum
                => EnumHelper.GetKeyValuePairs<T>();

            public static List<KeyValuePair<int, string>> GetIntKeyValuePairs<T>() where T : Enum
                => EnumHelper.GetIntKeyValuePairs<T>();
        }
    }
}