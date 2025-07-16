using LYBT.Common.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels {
    /// <summary>
    /// 通用分页列表视图模型基类
    /// </summary>
    public abstract class BaseListViewModel<T> : BindableBase {
        protected BaseListViewModel() {
            NextPageCommand = new DelegateCommand(async () => await LoadPageAsync(CurrentPage + 1),
                () => CurrentPage < TotalPages).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);
            PrevPageCommand = new DelegateCommand(async () => await LoadPageAsync(CurrentPage - 1),
                () => CurrentPage > 1).ObservesProperty(() => CurrentPage);
        }

        /// <summary>
        /// Items for the current page.
        /// </summary>
        public ObservableCollection<T> Items { get; } = new();

        /// <summary>
        /// Alias of <see cref="Items"/> for convenience.
        /// </summary>
        public ObservableCollection<T> PagedList => Items;

        private bool _isBusy;
        /// <summary>
        /// Gets or sets whether the list is loading data.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private int _currentPage = 1;
        /// <summary>
        /// Current page index (1-based).
        /// </summary>
        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }

        /// <summary>
        /// Alias of <see cref="CurrentPage"/>.
        /// </summary>
        public int PageIndex {
            get => CurrentPage;
            set => CurrentPage = value;
        }

        private int _totalPages = 1;
        /// <summary>
        /// Total number of pages.
        /// </summary>
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }

        /// <summary>
        /// Alias of <see cref="TotalPages"/>.
        /// </summary>
        public int TotalPage {
            get => TotalPages;
            set => TotalPages = value;
        }

        private int _totalCount;
        /// <summary>
        /// Total item count across all pages.
        /// </summary>
        public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }

        public int PageSize { get; set; } = 20;

        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand PrevPageCommand { get; }

        protected abstract Task<PagedResultDto<T>> GetPagedAsync(int page, int pageSize);

        public async Task LoadPageAsync(int page = 1) {
            if (page < 1) page = 1;
            IsBusy = true;
            try {
                var result = await GetPagedAsync(page, PageSize);
                Items.Clear();
                foreach (var item in result.Items)
                    Items.Add(item);
                CurrentPage = page;
                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize);
            }
            finally {
                IsBusy = false;
            }
        }
    }
}
