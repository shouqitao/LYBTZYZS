using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 搜索服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供搜索文本管理、搜索防抖、搜索执行功能
    /// </summary>
    public interface ISearchService : INotifyPropertyChanged
    {
        /// <summary>搜索文本</summary>
        string SearchText { get; set; }

        /// <summary>是否正在搜索</summary>
        bool IsSearching { get; }

        /// <summary>搜索防抖延迟（毫秒）</summary>
        int DebounceDelay { get; set; }

        /// <summary>
        /// 搜索请求事件（防抖后触发）
        /// </summary>
        event EventHandler<SearchRequestedEventArgs>? SearchRequested;

        /// <summary>
        /// 执行搜索
        /// </summary>
        /// <param name="searchAction">搜索操作</param>
        Task ExecuteSearchAsync(Func<string, Task> searchAction);

        /// <summary>
        /// 立即执行搜索（忽略防抖）
        /// </summary>
        /// <param name="searchAction">搜索操作</param>
        Task ExecuteSearchImmediateAsync(Func<string, Task> searchAction);

        /// <summary>清空搜索</summary>
        void ClearSearch();

        /// <summary>取消当前搜索</summary>
        void CancelSearch();
    }

    /// <summary>
    /// 搜索请求事件参数
    /// </summary>
    public class SearchRequestedEventArgs : EventArgs
    {
        /// <summary>搜索文本</summary>
        public string SearchText { get; }

        public SearchRequestedEventArgs(string searchText)
        {
            SearchText = searchText;
        }
    }
}
