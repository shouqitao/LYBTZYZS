using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.WPF.Client.Core.Models.Patients;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型
    /// </summary>
    public class PatientManagementViewModel : BindableBase
    {
        private readonly IPatientService _patientService;

        public PatientManagementViewModel(IPatientService patientService)
        {
            _patientService = patientService;
            InitializeCommands();
            _ = LoadPatientsAsync();
        }

        #region Properties

        private ObservableCollection<PatientInfo> _patients = new();
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                SetProperty(ref _totalCount, value);
                RaisePropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText => $"共 {TotalCount} 条记录";

        public string PageInfo => $"{CurrentPage} / {TotalPages}";

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; } = null!;
        public DelegateCommand ResetCommand { get; private set; } = null!;
        public DelegateCommand AddPatientCommand { get; private set; } = null!;
        public DelegateCommand EditPatientCommand { get; private set; } = null!;
        public DelegateCommand<PatientInfo> ViewPatientCommand { get; private set; } = null!;
        public DelegateCommand<PatientInfo> ViewRecordsCommand { get; private set; } = null!;
        public DelegateCommand DisablePatientCommand { get; private set; } = null!;
        public DelegateCommand EnablePatientCommand { get; private set; } = null!;
        public DelegateCommand ExportCommand { get; private set; } = null!;
        public DelegateCommand ImportCommand { get; private set; } = null!;
        public DelegateCommand FirstPageCommand { get; private set; } = null!;
        public DelegateCommand PreviousPageCommand { get; private set; } = null!;
        public DelegateCommand NextPageCommand { get; private set; } = null!;
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(async () => await SearchPatients());
            ResetCommand = new DelegateCommand(async () => await ResetSearch());
            AddPatientCommand = new DelegateCommand(AddPatient);
            EditPatientCommand = new DelegateCommand(EditPatient);
            ViewPatientCommand = new DelegateCommand<PatientInfo>(ViewPatient);
            ViewRecordsCommand = new DelegateCommand<PatientInfo>(ViewRecords);
            DisablePatientCommand = new DelegateCommand(async () => await DisablePatient());
            EnablePatientCommand = new DelegateCommand(async () => await EnablePatient());
            ExportCommand = new DelegateCommand(async () => await ExportPatients());
            ImportCommand = new DelegateCommand(async () => await ImportPatients());
            FirstPageCommand = new DelegateCommand(async () => await FirstPage(), CanFirstPage);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPage(), CanPreviousPage);
            NextPageCommand = new DelegateCommand(async () => await NextPage(), CanNextPage);
            LastPageCommand = new DelegateCommand(async () => await LastPage(), CanLastPage);
        }

        #endregion

        #region Command Implementations

        private async Task LoadPatientsAsync()
        {
            try
            {
                var query = new PatientPagedQueryDto
                {
                    SearchKeyword = SearchKeyword,
                    CurrentPage = CurrentPage,
                    PageSize = 20
                };

                var result = await _patientService.GetPagedAsync(query);
                
                Patients.Clear();
                foreach (var patient in result.Items)
                {
                    Patients.Add(patient);
                }

                TotalCount = result.TotalCount;
                TotalPages = result.TotalPages;
                CurrentPage = result.CurrentPage;
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    MessageBox.Show($"加载患者列表失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载患者列表时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SearchPatients()
        {
            CurrentPage = 1;
            await LoadPatientsAsync();
        }

        private async Task ResetSearch()
        {
            SearchKeyword = string.Empty;
            CurrentPage = 1;
            await LoadPatientsAsync();
        }

        private void AddPatient()
        {
            // TODO: 打开新增患者对话框
            MessageBox.Show("新增患者功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditPatient()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("请选择要编辑的患者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: 打开编辑患者对话框
            MessageBox.Show($"编辑患者：{SelectedPatient.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewPatient(PatientInfo patient)
        {
            if (patient == null) return;

            // TODO: 打开患者详情对话框
            MessageBox.Show($"查看患者详情：{patient.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewRecords(PatientInfo patient)
        {
            if (patient == null) return;

            // TODO: 打开患者病历列表
            MessageBox.Show($"查看患者病历：{patient.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DisablePatient()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("请选择要禁用的患者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要禁用患者 \"{SelectedPatient.Name}\" 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var response = await _patientService.DisableAsync(SelectedPatient.Id);
                if (response.IsSuccess)
                {
                    MessageBox.Show("患者已禁用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPatientsAsync();
                }
                else
                {
                    MessageBox.Show($"禁用患者失败：{response.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task EnablePatient()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("请选择要启用的患者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var response = await _patientService.EnableAsync(SelectedPatient.Id);
            if (response.IsSuccess)
            {
                MessageBox.Show("患者已启用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPatientsAsync();
            }
            else
            {
                MessageBox.Show($"启用患者失败：{response.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportPatients()
        {
            try
            {
                var result = await _patientService.ExportAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                        DefaultExt = "csv",
                        FileName = $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // TODO: 实现CSV导出
                        MessageBox.Show($"导出 {result.Data.Count} 条患者数据成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show($"导出患者数据失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ImportPatients()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    // TODO: 实现CSV导入
                    MessageBox.Show("导入功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Pagination

        private async Task FirstPage()
        {
            CurrentPage = 1;
            await LoadPatientsAsync();
            RaiseCanExecuteChanged();
        }

        private async Task PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadPatientsAsync();
                RaiseCanExecuteChanged();
            }
        }

        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadPatientsAsync();
                RaiseCanExecuteChanged();
            }
        }

        private async Task LastPage()
        {
            CurrentPage = TotalPages;
            await LoadPatientsAsync();
            RaiseCanExecuteChanged();
        }

        private bool CanFirstPage() => CurrentPage > 1;
        private bool CanPreviousPage() => CurrentPage > 1;
        private bool CanNextPage() => CurrentPage < TotalPages;
        private bool CanLastPage() => CurrentPage < TotalPages;

        private void RaiseCanExecuteChanged()
        {
            FirstPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(PageInfo));
        }

        #endregion
    }
}