using System;

namespace LYBT.Common.Models {
    /// <summary>
    /// Generic enum item for combo box binding
    /// </summary>
    public class EnumItem<T> where T : Enum {
        public EnumItem() {}
        public EnumItem(T value, string text) {
            Value = value;
            Text = text;
        }
/// <summary>
/// Value 属性。
/// </summary>
        public T Value { get; set; }
/// <summary>
/// Text 属性。
/// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
