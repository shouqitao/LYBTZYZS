namespace LYBT.Shared.Models.Common
{
    /// <summary>
    /// 枚举项模型 - 用于WPF ComboBox绑定等场景
    /// 前后端通用的枚举包装类
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    public class EnumItem<T> where T : Enum
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EnumItem()
        {
            Value = default!;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <param name="text">显示文本</param>
        public EnumItem(T value, string text)
        {
            Value = value;
            Text = text;
        }

        /// <summary>
        /// 枚举值
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 重写ToString方法，返回显示文本
        /// </summary>
        /// <returns>显示文本</returns>
        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// 重写Equals方法
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj)
        {
            if (obj is EnumItem<T> other)
            {
                return Equals(Value, other.Value);
            }
            return false;
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }
    }
}