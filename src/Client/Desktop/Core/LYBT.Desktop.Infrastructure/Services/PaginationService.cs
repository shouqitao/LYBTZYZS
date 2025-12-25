using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 分页服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    public partial class PaginationService : ObservableObject, IPaginationService
    {
        private static readonly int[] DefaultPageSizes = [10, 20, 50, 100];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        [NotifyPropertyChangedFor(nameof(CanGoToFirstPage))]
        [NotifyPropertyChangedFor(nameof(CanGoToPreviousPage))]
        [NotifyPropertyChangedFor(nameof(CanGoToNextPage))]
        [NotifyPropertyChangedFor(nameof(CanGoToLastPage))]
        private int _currentPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        private int _pageSize = 20;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        [NotifyPropertyChangedFor(nameof(CanGoToNextPage))]
        [NotifyPropertyChangedFor(nameof(CanGoToLastPage))]
        private int _totalCount;

        /// <inheritdoc/>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <inheritdoc/>
        public IReadOnlyList<int> PageSizes => DefaultPageSizes;

        /// <inheritdoc/>
        public bool CanGoToFirstPage => CurrentPage > 1;

        /// <inheritdoc/>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <inheritdoc/>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        /// <inheritdoc/>
        public bool CanGoToLastPage => CurrentPage < TotalPages;

        /// <inheritdoc/>
        public event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <inheritdoc/>
        public void GoToFirstPage()
        {
            if (CanGoToFirstPage)
            {
                GoToPage(1);
            }
        }

        /// <inheritdoc/>
        public void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                GoToPage(CurrentPage - 1);
            }
        }

        /// <inheritdoc/>
        public void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                GoToPage(CurrentPage + 1);
            }
        }

        /// <inheritdoc/>
        public void GoToLastPage()
        {
            if (CanGoToLastPage)
            {
                GoToPage(TotalPages);
            }
        }

        /// <inheritdoc/>
        public void GoToPage(int page)
        {
            if (page < 1) page = 1;
            if (page > TotalPages && TotalPages > 0) page = TotalPages;

            var oldPage = CurrentPage;
            if (oldPage != page)
            {
                CurrentPage = page;
                PageChanged?.Invoke(this, new PageChangedEventArgs(oldPage, page, PageSize));
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            CurrentPage = 1;
            TotalCount = 0;
        }

        partial void OnPageSizeChanged(int oldValue, int newValue)
        {
            if (oldValue != newValue && oldValue > 0)
            {
                // 调整当前页以保持大致相同的位置
                var firstItemIndex = (CurrentPage - 1) * oldValue;
                var newPage = newValue > 0 ? (firstItemIndex / newValue) + 1 : 1;
                CurrentPage = Math.Max(1, Math.Min(newPage, TotalPages > 0 ? TotalPages : 1));
                PageChanged?.Invoke(this, new PageChangedEventArgs(CurrentPage, CurrentPage, newValue));
            }
        }
    }
}
