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

        public ObservableCollection<T> Items { get; } = new();

        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }

        private int _totalPages = 1;
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }

        public int PageSize { get; set; } = 20;

        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand PrevPageCommand { get; }

        protected abstract Task<PagedResultDto<T>> GetPagedAsync(int page, int pageSize);

        public async Task LoadPageAsync(int page = 1) {
            if (page < 1) page = 1;
            var result = await GetPagedAsync(page, PageSize);
            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);
            CurrentPage = page;
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize);
        }
    }
}
