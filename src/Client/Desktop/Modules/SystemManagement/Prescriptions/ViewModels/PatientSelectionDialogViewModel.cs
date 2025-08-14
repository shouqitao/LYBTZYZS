using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Admin.Prescriptions.ViewModels
{
    /// <summary>
    /// 患者选择对话框视图模型
    /// </summary>
    public class PatientSelectionDialogViewModel : BindableBase
    {
        #region 字段

        private readonly IPatientService _patientService;
        private string _searchKeyword = string.Empty;
        private SearchTypeOption _selectedSearchType;
        private PatientInfo? _selectedPatient;
        private ObservableCollection<PatientInfo> _allPatients;
        private ObservableCollection<PatientInfo> _filteredPatients;

        #endregion

        #region 属性

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    _ = SearchPatientsAsync();
                }
            }
        }

        /// <summary>
        /// 搜索类型选项
        /// </summary>
        public ObservableCollection<SearchTypeOption> SearchTypes { get; }

        /// <summary>
        /// 选中的搜索类型
        /// </summary>
        public SearchTypeOption SelectedSearchType
        {
            get => _selectedSearchType;
            set => SetProperty(ref _selectedSearchType, value);
        }

        /// <summary>
        /// 选中的患者
        /// </summary>
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    RaisePropertyChanged(nameof(HasSelectedPatient));
                }
            }
        }

        /// <summary>
        /// 所有患者列表
        /// </summary>
        public ObservableCollection<PatientInfo> AllPatients
        {
            get => _allPatients;
            set => SetProperty(ref _allPatients, value);
        }

        /// <summary>
        /// 过滤后的患者列表
        /// </summary>
        public ObservableCollection<PatientInfo> FilteredPatients
        {
            get => _filteredPatients;
            set => SetProperty(ref _filteredPatients, value);
        }

        /// <summary>
        /// 是否有选中的患者
        /// </summary>
        public bool HasSelectedPatient => SelectedPatient != null;

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; set; }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
        public DelegateCommand<PatientInfo> SelectPatientCommand { get; }
        public DelegateCommand CreateNewPatientCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public PatientSelectionDialogViewModel(IPatientService patientService)
        {
            _patientService = patientService;
            _allPatients = new ObservableCollection<PatientInfo>();
            _filteredPatients = new ObservableCollection<PatientInfo>();

            // 初始化搜索类型
            SearchTypes = new ObservableCollection<SearchTypeOption>
            {
                new SearchTypeOption { Value = "All", Display = "全部" },
                new SearchTypeOption { Value = "Name", Display = "姓名" },
                new SearchTypeOption { Value = "Phone", Display = "手机号" },
                new SearchTypeOption { Value = "MedicalRecord", Display = "病历号" },
                new SearchTypeOption { Value = "IdCard", Display = "身份证" }
            };
            _selectedSearchType = SearchTypes.First();

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchPatientsAsync());
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch);
            SelectPatientCommand = new DelegateCommand<PatientInfo>(ExecuteSelectPatient);
            CreateNewPatientCommand = new DelegateCommand(ExecuteCreateNewPatient);
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm)
                .ObservesProperty(() => SelectedPatient);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 加载初始数据
            _ = LoadPatientsAsync();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取选中的患者
        /// </summary>
        public PatientInfo? GetSelectedPatient()
        {
            return SelectedPatient;
        }

        #endregion

        #region 命令实现

        private async Task SearchPatientsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                FilteredPatients = new ObservableCollection<PatientInfo>(AllPatients);
                return;
            }

            await Task.Run(() =>
            {
                var keyword = SearchKeyword.Trim().ToLower();
                var filtered = AllPatients.AsEnumerable();

                // 根据搜索类型过滤
                switch (SelectedSearchType.Value)
                {
                    case "Name":
                        filtered = filtered.Where(p => p.Name != null && p.Name.ToLower().Contains(keyword));
                        break;
                    case "Phone":
                        filtered = filtered.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(keyword));
                        break;
                    case "MedicalRecord":
                        // 如果没有病历号字段，使用ID代替
                        filtered = filtered.Where(p => p.Id.ToString().ToLower().Contains(keyword));
                        break;
                    case "IdCard":
                        filtered = filtered.Where(p => p.IdNumber != null && p.IdNumber.Contains(keyword));
                        break;
                    default: // All
                        filtered = filtered.Where(p =>
                            (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                            (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                            p.Id.ToString().ToLower().Contains(keyword) ||
                            (p.IdNumber != null && p.IdNumber.Contains(keyword)));
                        break;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredPatients = new ObservableCollection<PatientInfo>(filtered);
                    
                    // 如果只有一个结果，自动选中
                    if (FilteredPatients.Count == 1)
                    {
                        SelectedPatient = FilteredPatients.First();
                    }
                });
            });
        }

        private void ExecuteClearSearch()
        {
            SearchKeyword = string.Empty;
            SelectedSearchType = SearchTypes.First();
            FilteredPatients = new ObservableCollection<PatientInfo>(AllPatients);
        }

        private void ExecuteSelectPatient(PatientInfo? patient)
        {
            if (patient != null)
            {
                SelectedPatient = patient;
                ExecuteConfirm();
            }
        }

        private void ExecuteCreateNewPatient()
        {
            // TODO: 打开新建患者对话框
            // 这里可以导航到患者管理模块的新建功能
        }

        private bool CanExecuteConfirm()
        {
            return SelectedPatient != null;
        }

        private void ExecuteConfirm()
        {
            DialogResult = true;
        }

        private void ExecuteCancel()
        {
            DialogResult = false;
            SelectedPatient = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载患者列表
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                // 使用分页查询获取患者列表
                var queryDto = new LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取前1000个患者
                    Keyword = string.Empty
                };
                var result = await _patientService.GetPagedAsync(queryDto);
                if (result != null && result.Items != null)
                {
                    AllPatients = new ObservableCollection<PatientInfo>(result.Items);
                    FilteredPatients = new ObservableCollection<PatientInfo>(result.Items);
                }
            }
            catch (Exception ex)
            {
                // 错误处理
                System.Diagnostics.Debug.WriteLine($"加载患者列表失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 搜索类型选项
    /// </summary>
    public class SearchTypeOption
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }
}