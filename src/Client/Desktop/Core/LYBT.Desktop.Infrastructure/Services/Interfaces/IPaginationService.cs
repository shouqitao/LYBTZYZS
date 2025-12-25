using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 分页服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供分页状态管理、页面导航功能
    /// </summary>
    public interface IPaginationService : INotifyPropertyChanged
    {
        /// <summary>当前页码（从1开始）</summary>
        int CurrentPage { get; set; }

        /// <summary>每页条数</summary>
        int PageSize { get; set; }

        /// <summary>总记录数</summary>
        int TotalCount { get; set; }

        /// <summary>总页数</summary>
        int TotalPages { get; }

        /// <summary>可选的每页条数列表</summary>
        IReadOnlyList<int> PageSizes { get; }

        /// <summary>是否可以跳转到首页</summary>
        bool CanGoToFirstPage { get; }

        /// <summary>是否可以跳转到上一页</summary>
        bool CanGoToPreviousPage { get; }

        /// <summary>是否可以跳转到下一页</summary>
        bool CanGoToNextPage { get; }

        /// <summary>是否可以跳转到末页</summary>
        bool CanGoToLastPage { get; }

        /// <summary>
        /// 页面变更事件
        /// </summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <summary>跳转到首页</summary>
        void GoToFirstPage();

        /// <summary>跳转到上一页</summary>
        void GoToPreviousPage();

        /// <summary>跳转到下一页</summary>
        void GoToNextPage();

        /// <summary>跳转到末页</summary>
        void GoToLastPage();

        /// <summary>跳转到指定页</summary>
        /// <param name="page">目标页码</param>
        void GoToPage(int page);

        /// <summary>重置分页状态</summary>
        void Reset();
    }

    /// <summary>
    /// 页面变更事件参数
    /// </summary>
    public class PageChangedEventArgs : EventArgs
    {
        /// <summary>旧页码</summary>
        public int OldPage { get; }

        /// <summary>新页码</summary>
        public int NewPage { get; }

        /// <summary>每页条数</summary>
        public int PageSize { get; }

        public PageChangedEventArgs(int oldPage, int newPage, int pageSize)
        {
            OldPage = oldPage;
            NewPage = newPage;
            PageSize = pageSize;
        }
    }
}
