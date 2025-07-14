using System;
using System.Collections.ObjectModel;
using LYBT.Common.Extensions;
using LYBT.Common.Models;

using System.ComponentModel;

namespace LYBT.Common.Helpers {
    /// <summary>
    /// Helper methods for working with enums.
    /// </summary>
    [Description("枚举工具类")]
    public static class EnumHelper {
        /// <summary>
        /// Build an observable collection suitable for binding to a ComboBox.
        /// Each item contains the enum value and its description text.
        /// </summary>
        public static ObservableCollection<EnumItem<T>> BuildComboBoxSource<T>() where T : Enum {
            var list = new ObservableCollection<EnumItem<T>>();
            foreach (T value in Enum.GetValues(typeof(T))) {
                list.Add(new EnumItem<T>(value, value.GetDescription()));
            }
            return list;
        }
    }
}
