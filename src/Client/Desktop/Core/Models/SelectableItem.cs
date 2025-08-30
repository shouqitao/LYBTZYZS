using LYBT.Shared.Models.Contracts.Common;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Models
{
    /// <summary>
    /// 可选择项包装类，用于支持列表中的选择功能
    /// </summary>
    /// <typeparam name="T">包装的数据类型</typeparam>
    public class SelectableItem<T> : BindableBase
    {
        private bool _isSelected;
        private T _data = default!;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 包装的数据
        /// </summary>
        public T Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">要包装的数据</param>
        /// <param name="isSelected">初始选中状态</param>
        public SelectableItem(T data, bool isSelected = false)
        {
            Data = data;
            IsSelected = isSelected;
        }
    }
}