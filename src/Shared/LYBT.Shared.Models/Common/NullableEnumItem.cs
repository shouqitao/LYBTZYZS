namespace LYBT.Shared.Models.Common {

    /// <summary>
    /// 可空枚举项模型 - 用于WPF ComboBox绑定等场景（支持空选项）
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    public class NullableEnumItem<T> where T : struct, Enum {

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public NullableEnumItem() {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">枚举值（可为null）</param>
        /// <param name="text">显示文本</param>
        public NullableEnumItem(T? value, string text) {
            Value = value;
            Text = text;
        }

        /// <summary>
        /// 枚举值（可为null）
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 重写ToString方法，返回显示文本
        /// </summary>
        /// <returns>显示文本</returns>
        public override string ToString() {
            return Text;
        }

        /// <summary>
        /// 重写Equals方法
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj) {
            if (obj is NullableEnumItem<T> other) {
                return Equals(Value, other.Value);
            }
            return false;
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode() {
            return Value?.GetHashCode() ?? 0;
        }
    }
}