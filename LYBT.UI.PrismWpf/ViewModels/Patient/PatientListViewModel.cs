using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels.Patient
{
    /// <summary>
    /// 患者列表ViewModel
    /// </summary>
    public class PatientListViewModel : BindableBase
    {
        #region Fields
        private ObservableCollection<PatientInfo> _patients = new();
        private PatientInfo? _selectedPatient;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        #endregion

        #region Properties
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
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
        public ICommand AddPatientCommand { get; private set; }
        public ICommand EditPatientCommand { get; private set; }
        public ICommand DeletePatientCommand { get; private set; }
        public ICommand ViewRecordsCommand { get; private set; }
        public ICommand BatchImportCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        #endregion

        #region Constructor
        public PatientListViewModel()
        {
            InitializeCommands();
            LoadData();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            AddPatientCommand = new DelegateCommand(OnAddPatient);
            EditPatientCommand = new DelegateCommand<PatientInfo>(OnEditPatient);
            DeletePatientCommand = new DelegateCommand<PatientInfo>(OnDeletePatient);
            ViewRecordsCommand = new DelegateCommand<PatientInfo>(OnViewRecords);
            BatchImportCommand = new DelegateCommand(OnBatchImport);
            SearchCommand = new DelegateCommand(OnSearch);
            PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        }

        private async void LoadData()
        {
            // TODO: 调用API加载患者数据
            await Task.Delay(100);
        }

        private void OnAddPatient() { /* TODO */ }
        private void OnEditPatient(PatientInfo? patient) { /* TODO */ }
        private void OnDeletePatient(PatientInfo? patient) { /* TODO */ }
        private void OnViewRecords(PatientInfo? patient) { /* TODO */ }
        private void OnBatchImport() { /* TODO */ }
        private void OnSearch() { /* TODO */ }
        private void OnPreviousPage() { /* TODO */ }
        private void OnNextPage() { /* TODO */ }
        #endregion
    }
}