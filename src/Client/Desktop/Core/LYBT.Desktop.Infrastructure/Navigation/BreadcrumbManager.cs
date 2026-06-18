using System.Collections.ObjectModel;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// 面包屑管理器 - 维护面包屑导航路径的可观察集合
    /// </summary>
    internal sealed class BreadcrumbManager
    {
        private readonly ObservableCollection<BreadcrumbItem> _items = new();

        /// <summary>
        /// 面包屑列表（只读可观察集合）
        /// </summary>
        public ReadOnlyObservableCollection<BreadcrumbItem> Items { get; }

        public BreadcrumbManager()
        {
            Items = new ReadOnlyObservableCollection<BreadcrumbItem>(_items);
        }

        /// <summary>
        /// 用给定条目替换整个面包屑列表
        /// </summary>
        public void Replace(IEnumerable<BreadcrumbItem> items)
        {
            _items.Clear();
            foreach (var item in items)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// 清除所有面包屑
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }
    }
}
