using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels.Doctor
{
    /// <summary>
    /// 医生列表ViewModel
    /// </summary>
    public class DoctorListViewModel : BindableBase
    {
        #region Fields
        private ObservableCollection<DoctorInfo> _doctors = new();
        private DoctorInfo? _selectedDoctor;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        #endregion

        #region Properties
        public ObservableCollection<DoctorInfo> Doctors
        {
            get => _doctors;
            set => SetProperty(ref _doctors, value);
        }

        public DoctorInfo? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetProperty(ref _selectedDoctor, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }
        #endregion

        #region Commands
        public ICommand AddDoctorCommand { get; private set; }
        public ICommand EditDoctorCommand { get; private set; }
        public ICommand ToggleActiveCommand { get; private set; }
        public ICommand ManageScheduleCommand { get; private set; }
        public ICommand BatchImportCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        #endregion

        #region Constructor
        public DoctorListViewModel()
        {
            InitializeCommands();
            LoadData();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            AddDoctorCommand = new DelegateCommand(OnAddDoctor);
            EditDoctorCommand = new DelegateCommand<DoctorInfo>(OnEditDoctor);
            ToggleActiveCommand = new DelegateCommand<DoctorInfo>(OnToggleActive);
            ManageScheduleCommand = new DelegateCommand<DoctorInfo>(OnManageSchedule);
            BatchImportCommand = new DelegateCommand(OnBatchImport);
            SearchCommand = new DelegateCommand(OnSearch);
            PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        }

        private async void LoadData()
        {
            // TODO: 调用API加载医生数据
            await Task.Delay(100);
        }

        private void OnAddDoctor() { /* TODO */ }
        private void OnEditDoctor(DoctorInfo? doctor) { /* TODO */ }
        private void OnToggleActive(DoctorInfo? doctor) { /* TODO */ }
        private void OnManageSchedule(DoctorInfo? doctor) { /* TODO */ }
        private void OnBatchImport() { /* TODO */ }
        private void OnSearch() { /* TODO */ }
        private void OnPreviousPage() { /* TODO */ }
        private void OnNextPage() { /* TODO */ }
        #endregion
    }
}