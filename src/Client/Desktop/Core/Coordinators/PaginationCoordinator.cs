using Prism.Mvvm;
using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Coordinators
{
    /// <summary>
    /// 分页协调器实现 - 管理分页状态和导航逻辑
    /// UltraThink架构: 将分页职责从ViewModel中分离，实现单一职责原则
    /// </summary>
    public class PaginationCoordinator : BindableBase, IPaginationCoordinator
    {
        #region Fields

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;

        #endregion

        #region Properties

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    RaisePropertyChanged(nameof(CanGoToNextPage));
                }
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    UpdateTotalPages();
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdateTotalPages();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _pageSize > 0 ? (int)Math.Ceiling((double)_totalCount / _pageSize) : 0;

        /// <summary>
        /// 是否可以转到上一页
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以转到下一页
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        #endregion

        #region Events

        /// <summary>
        /// 页码变化事件
        /// </summary>
        public event EventHandler<PageChangedEventArgs>? PageChanged;

        #endregion

        #region Methods

        /// <summary>
        /// 跳转到第一页
        /// </summary>
        public async Task GoToFirstPageAsync()
        {
            await GoToPageAsync(1);
        }

        /// <summary>
        /// 跳转到上一页
        /// </summary>
        public async Task GoToPreviousPageAsync()
        {
            if (CanGoToPreviousPage)
            {
                await GoToPageAsync(CurrentPage - 1);
            }
        }

        /// <summary>
        /// 跳转到下一页
        /// </summary>
        public async Task GoToNextPageAsync()
        {
            if (CanGoToNextPage)
            {
                await GoToPageAsync(CurrentPage + 1);
            }
        }

        /// <summary>
        /// 跳转到最后一页
        /// </summary>
        public async Task GoToLastPageAsync()
        {
            await GoToPageAsync(TotalPages);
        }

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        public async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage)
                return;

            var oldPage = CurrentPage;
            CurrentPage = page;

            // 触发页码变化事件
            var args = new PageChangedEventArgs(oldPage, page, PageSize);
            PageChanged?.Invoke(this, args);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 重置分页状态
        /// </summary>
        public void Reset()
        {
            CurrentPage = 1;
            TotalCount = 0;
        }

        /// <summary>
        /// 更新分页信息
        /// </summary>
        public void UpdatePagination(int totalCount)
        {
            TotalCount = totalCount;
            
            // 如果当前页超出范围，调整到最后一页
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新总页数并触发相关属性变化
        /// </summary>
        private void UpdateTotalPages()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CanGoToPreviousPage));
            RaisePropertyChanged(nameof(CanGoToNextPage));
        }

        #endregion
    }
}