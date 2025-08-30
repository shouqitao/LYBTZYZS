using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Coordinators
{
    /// <summary>
    /// 分页协调器接口 - 负责分页逻辑的统一管理
    /// UltraThink架构: 将分页逻辑从ViewModel中分离出来
    /// </summary>
    public interface IPaginationCoordinator : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// 当前页码
        /// </summary>
        int CurrentPage { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        int TotalCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// 是否可以转到上一页
        /// </summary>
        bool CanGoToPreviousPage { get; }

        /// <summary>
        /// 是否可以转到下一页
        /// </summary>
        bool CanGoToNextPage { get; }

        #endregion

        #region Events

        /// <summary>
        /// 页码变化事件
        /// </summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;

        #endregion

        #region Methods

        /// <summary>
        /// 跳转到第一页
        /// </summary>
        Task GoToFirstPageAsync();

        /// <summary>
        /// 跳转到上一页
        /// </summary>
        Task GoToPreviousPageAsync();

        /// <summary>
        /// 跳转到下一页
        /// </summary>
        Task GoToNextPageAsync();

        /// <summary>
        /// 跳转到最后一页
        /// </summary>
        Task GoToLastPageAsync();

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        Task GoToPageAsync(int page);

        /// <summary>
        /// 重置分页状态
        /// </summary>
        void Reset();

        /// <summary>
        /// 更新分页信息
        /// </summary>
        void UpdatePagination(int totalCount);

        #endregion
    }

    /// <summary>
    /// 页码变化事件参数
    /// </summary>
    public class PageChangedEventArgs : EventArgs
    {
        public int OldPage { get; }
        public int NewPage { get; }
        public int PageSize { get; }

        public PageChangedEventArgs(int oldPage, int newPage, int pageSize)
        {
            OldPage = oldPage;
            NewPage = newPage;
            PageSize = pageSize;
        }
    }
}