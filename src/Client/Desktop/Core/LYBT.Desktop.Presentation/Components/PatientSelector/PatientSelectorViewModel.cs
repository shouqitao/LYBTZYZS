using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Presentation.Components.PatientSelector
{
    /// <summary>
    /// 患者选择器 ViewModel
    /// </summary>
    public class PatientSelectorViewModel : BindableBase, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private CancellationTokenSource? _searchCancellationTokenSource;

        #region 属性

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNoResults)));
                    _ = SearchWithDebounceAsync();
                }
            }
        }

        private ObservableCollection<object> _searchResults = new();
        /// <summary>
        /// 搜索结果列表
        /// </summary>
        public ObservableCollection<object> SearchResults
        {
            get => _searchResults;
            set
            {
                if (SetProperty(ref _searchResults, value))
                {
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNoResults)));
                }
            }
        }

        /// <summary>
        /// 是否无搜索结果
        /// </summary>
        public bool HasNoResults => SearchResults.Count == 0 && !string.IsNullOrEmpty(SearchKeyword);

        private object? _selectedPatient;
        /// <summary>
        /// 选中的患者
        /// </summary>
        public object? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private bool _showQuickCreate = false;
        /// <summary>
        /// 是否显示快速创建面板
        /// </summary>
        public bool ShowQuickCreate
        {
            get => _showQuickCreate;
            set => SetProperty(ref _showQuickCreate, value);
        }

        private string _newPatientName = string.Empty;
        /// <summary>
        /// 新患者姓名
        /// </summary>
        public string NewPatientName
        {
            get => _newPatientName;
            set => SetProperty(ref _newPatientName, value);
        }

        private string _newPatientGender = string.Empty;
        /// <summary>
        /// 新患者性别
        /// </summary>
        public string NewPatientGender
        {
            get => _newPatientGender;
            set => SetProperty(ref _newPatientGender, value);
        }

        private string _newPatientPhone = string.Empty;
        /// <summary>
        /// 新患者手机号
        /// </summary>
        public string NewPatientPhone
        {
            get => _newPatientPhone;
            set => SetProperty(ref _newPatientPhone, value);
        }

        private bool _isLoading = false;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasError)));
                }
            }
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; private set; }

        /// <summary>
        /// 选择患者命令
        /// </summary>
        public DelegateCommand<object?> SelectPatientCommand { get; private set; }

        /// <summary>
        /// 快速创建命令
        /// </summary>
        public DelegateCommand QuickCreateCommand { get; private set; }

        /// <summary>
        /// 切换快速创建面板命令
        /// </summary>
        public DelegateCommand ToggleQuickCreateCommand { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 PatientSelectorViewModel
        /// </summary>
        /// <param name="eventAggregator">事件聚合器</param>
        public PatientSelectorViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => !string.IsNullOrEmpty(SearchKeyword));
            SelectPatientCommand = new DelegateCommand<object?>(SelectPatient, patient => patient != null);
            QuickCreateCommand = new DelegateCommand(async () => await QuickCreateAsync(), CanQuickCreate);
            ToggleQuickCreateCommand = new DelegateCommand(() => ShowQuickCreate = !ShowQuickCreate);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 带防抖的搜索
        /// </summary>
        private async Task SearchWithDebounceAsync()
        {
            // 取消之前的搜索
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 300ms 防抖
                await Task.Delay(300, _searchCancellationTokenSource.Token);
                
                if (!string.IsNullOrEmpty(SearchKeyword))
                {
                    await SearchAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // 搜索被取消，忽略
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task SearchAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // 临时模拟搜索结果
                await Task.Delay(500); // 模拟网络延迟
                SearchResults.Clear();
                
                // 模拟一些搜索结果
                var mockResults = new[]
                {
                    new { Id = Guid.NewGuid(), Name = $"张三 ({SearchKeyword})", Gender = "男", Age = 35, PhoneNumber = "13800138001" },
                    new { Id = Guid.NewGuid(), Name = $"李四 ({SearchKeyword})", Gender = "女", Age = 28, PhoneNumber = "13800138002" }
                };

                foreach (var result in mockResults)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"搜索失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 选择患者
        /// </summary>
        private void SelectPatient(object? patient)
        {
            if (patient == null) return;

            try
            {
                // 发布患者选择事件
                var payload = CreatePatientSelectedPayload(patient);
                _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);

                // 清空搜索结果
                SearchKeyword = string.Empty;
                SearchResults.Clear();
                ShowQuickCreate = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"选择患者失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 创建患者选择事件负载
        /// </summary>
        private PatientSelectedPayload CreatePatientSelectedPayload(object patient)
        {
            // 临时创建负载
            var patientType = patient.GetType();
            var idProperty = patientType.GetProperty("Id")?.GetValue(patient) as Guid? ?? Guid.NewGuid();
            var nameProperty = patientType.GetProperty("Name")?.GetValue(patient)?.ToString() ?? "未知患者";
            var genderProperty = patientType.GetProperty("Gender")?.GetValue(patient)?.ToString() ?? "未知";
            var ageProperty = patientType.GetProperty("Age")?.GetValue(patient);
            var age = ageProperty is int ageValue ? ageValue : 0;
            var phoneProperty = patientType.GetProperty("PhoneNumber")?.GetValue(patient)?.ToString() ?? "";

            return new PatientSelectedPayload
            {
                PatientId = idProperty,
                PatientName = nameProperty,
                Gender = genderProperty,
                Age = age,
                PhoneNumber = phoneProperty,
                LastVisitDate = DateTime.Now.AddDays(-30),
                VisitCount = 1,
                AllergyHistory = "无",
                SelectedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 快速创建患者
        /// </summary>
        private async Task QuickCreateAsync()
        {
            if (!CanQuickCreate()) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // 验证手机号是否重复（简化验证）
                var phoneExists = SearchResults.Any(p => 
                {
                    var phoneProp = p.GetType().GetProperty("PhoneNumber")?.GetValue(p)?.ToString();
                    return phoneProp == NewPatientPhone;
                });

                if (phoneExists)
                {
                    ErrorMessage = "该手机号已存在，请选择现有患者或使用其他手机号";
                    return;
                }

                // 模拟创建患者
                await Task.Delay(300);
                var newPatient = new
                {
                    Id = Guid.NewGuid(),
                    Name = NewPatientName,
                    Gender = NewPatientGender,
                    Age = 25, // 默认年龄
                    PhoneNumber = NewPatientPhone
                };

                // 清空创建表单
                NewPatientName = string.Empty;
                NewPatientGender = string.Empty;
                NewPatientPhone = string.Empty;
                ShowQuickCreate = false;

                // 自动选择新创建的患者
                SelectPatient(newPatient);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"创建患者失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 是否可以快速创建
        /// </summary>
        private bool CanQuickCreate()
        {
            return !string.IsNullOrWhiteSpace(NewPatientName) &&
                   !string.IsNullOrWhiteSpace(NewPatientGender) &&
                   !string.IsNullOrWhiteSpace(NewPatientPhone) &&
                   NewPatientPhone.Length >= 11 &&
                   !IsLoading;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
        }

        #endregion
    }
}