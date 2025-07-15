using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Base {
    /// <summary>
    /// Provides a paged list view model base.
    /// </summary>
    /// <typeparam name="TDto">Dto type displayed in the list.</typeparam>
    public abstract class BaseListViewModel<TDto> : BindableBase {
        /// <summary>Collection of items for current page.</summary>
        public ObservableCollection<TDto> Items { get; } = new();

        private int _pageIndex = 1;
        /// <summary>Current page index (1-based).</summary>
        public int PageIndex {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        private int _pageSize = 20;
        /// <summary>Number of items per page.</summary>
        public int PageSize {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        /// <summary>Total item count.</summary>
        public int TotalCount {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>Command loading the current page.</summary>
        public DelegateCommand LoadPageCommand { get; }

        protected BaseListViewModel() {
            LoadPageCommand = new DelegateCommand(async () => await LoadPageAsync());
        }

        /// <summary>
        /// Loads items for the current page. Derived classes should override to
        /// provide data retrieval logic.
        /// </summary>
        public virtual Task LoadPageAsync() => Task.CompletedTask;
    }
}
