using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.WPF.Client.Modules.SystemManagement.Consultations.ViewModels
{
    /// <summary>
    /// 看诊记录管理视图模型 - 简化版
    /// </summary>
    public class ConsultationManagementViewModel : BindableBase
    {
        #region 属性

        private ObservableCollection<ConsultationInfo> _consultations;
        public ObservableCollection<ConsultationInfo> Consultations
        {
            get => _consultations;
            set => SetProperty(ref _consultations, value);
        }

        private ConsultationInfo _selectedConsultation;
        public ConsultationInfo SelectedConsultation
        {
            get => _selectedConsultation;
            set => SetProperty(ref _selectedConsultation, value);
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchConsultations();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadConsultationsAsync();
                }
            }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    _ = LoadConsultationsAsync();
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        #endregion

        #region 命令

        public ICommand LoadCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        #endregion

        public ConsultationManagementViewModel()
        {
            Consultations = new ObservableCollection<ConsultationInfo>();

            // 初始化命令
            LoadCommand = new DelegateCommand(async () => await LoadConsultationsAsync());
            SearchCommand = new DelegateCommand(SearchConsultations);
            ViewCommand = new DelegateCommand<ConsultationInfo>(ViewConsultation);
            DeleteCommand = new DelegateCommand<ConsultationInfo>(async (c) => await DeleteConsultationAsync(c));
            ExportCommand = new DelegateCommand(ExportConsultations);
            PrintCommand = new DelegateCommand<ConsultationInfo>(PrintConsultation);
            RefreshCommand = new DelegateCommand(async () => await LoadConsultationsAsync());
            PreviousPageCommand = new DelegateCommand(() => CurrentPage--, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(() => CurrentPage++, () => CurrentPage * PageSize < TotalCount);

            // 加载初始数据
            _ = LoadConsultationsAsync();
        }

        #region 方法

        private async Task LoadConsultationsAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(500); // 模拟加载

                // 创建示例数据
                var sampleData = new List<ConsultationInfo>();
                for (int i = 0; i < 5; i++)
                {
                    sampleData.Add(new ConsultationInfo
                    {
                        Id = Guid.NewGuid(),
                        PatientId = Guid.NewGuid(),
                        PatientName = $"患者{i + 1}",
                        PatientGender = i % 2 == 0 ? "男" : "女",
                        PatientAge = 30 + i * 5,
                        ConsultationTime = DateTime.Now.AddDays(-i),
                        ChiefComplaint = $"主诉症状{i + 1}",
                        TCMDiagnosis = $"中医诊断{i + 1}",
                        WesternDiagnosis = $"西医诊断{i + 1}",
                        DoctorName = "张医生",
                        CreateTime = DateTime.Now.AddDays(-i)
                    });
                }

                Consultations = new ObservableCollection<ConsultationInfo>(sampleData);
                TotalCount = sampleData.Count;
            }
            catch (Exception ex)
            {
                // 简单的错误处理
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SearchConsultations()
        {
            CurrentPage = 1;
            _ = LoadConsultationsAsync();
        }

        private void ViewConsultation(ConsultationInfo consultation)
        {
            if (consultation == null) return;
            // 简化版 - 直接显示信息
        }

        private async Task DeleteConsultationAsync(ConsultationInfo consultation)
        {
            if (consultation == null) return;

            try
            {
                IsLoading = true;
                await Task.Delay(300); // 模拟删除
                
                Consultations.Remove(consultation);
                TotalCount--;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportConsultations()
        {
        }

        private void PrintConsultation(ConsultationInfo consultation)
        {
            if (consultation == null) return;
        }

        #endregion
    }
}