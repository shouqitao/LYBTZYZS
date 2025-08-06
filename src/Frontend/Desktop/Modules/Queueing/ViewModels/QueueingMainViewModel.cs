using LYBT.WPF.Client.Core.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.Queueing.ViewModels
{
    /// <summary>
    /// 排队管理视图模型
    /// </summary>
    public class QueueingMainViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IApiService _apiService;
        private bool _isLoading;
        private string _selectedDepartment = "全部";

        public QueueingMainViewModel(IApiService apiService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _apiService = apiService;
            
            RefreshCommand = new DelegateCommand(ExecuteRefreshCommand);
            CallNextCommand = new DelegateCommand<object>(ExecuteCallNextCommand);
            SkipPatientCommand = new DelegateCommand<object>(ExecuteSkipPatientCommand);
            ResetQueueCommand = new DelegateCommand(ExecuteResetQueueCommand);
            
            WaitingQueue = new ObservableCollection<dynamic>();
            ProcessingQueue = new ObservableCollection<dynamic>();
            CompletedQueue = new ObservableCollection<dynamic>();
            Departments = new ObservableCollection<string> { "全部", "内科", "外科", "儿科", "妇科", "中医科" };
            
            LoadQueueData();
        }

        #region Properties

        public ObservableCollection<dynamic> WaitingQueue { get; }
        public ObservableCollection<dynamic> ProcessingQueue { get; }
        public ObservableCollection<dynamic> CompletedQueue { get; }
        public ObservableCollection<string> Departments { get; }

        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set 
            { 
                SetProperty(ref _selectedDepartment, value);
                LoadQueueData();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<object> CallNextCommand { get; }
        public DelegateCommand<object> SkipPatientCommand { get; }
        public DelegateCommand ResetQueueCommand { get; }

        #endregion

        #region Command Handlers

        private void ExecuteRefreshCommand()
        {
            LoadQueueData();
        }

        private void ExecuteCallNextCommand(object parameter)
        {
            if (WaitingQueue.Count > 0)
            {
                var nextPatient = WaitingQueue[0];
                WaitingQueue.RemoveAt(0);
                ProcessingQueue.Add(nextPatient);
                
                _commonDialogService.ShowInformationAsync($"已呼叫患者：{nextPatient.PatientName}", "呼叫患者").GetAwaiter().GetResult();
            }
            else
            {
                _commonDialogService.ShowInformationAsync("当前没有等待的患者", "提示").GetAwaiter().GetResult();
            }
        }

        private void ExecuteSkipPatientCommand(object parameter)
        {
            if (parameter != null)
            {
                var result = _commonDialogService.ShowConfirmationAsync("确定要跳过这位患者吗？", "确认跳过").GetAwaiter().GetResult();
                
                if (result )
                {
                    // TODO: 实现跳过患者逻辑
                    _commonDialogService.ShowInformationAsync("跳过患者功能待实现", "提示").GetAwaiter().GetResult();
                }
            }
        }

        private void ExecuteResetQueueCommand()
        {
            var result = _commonDialogService.ShowConfirmationAsync("确定要重置排队队列吗？这将清空所有排队信息。", "确认重置").GetAwaiter().GetResult();
            
            if (result )
            {
                WaitingQueue.Clear();
                ProcessingQueue.Clear();
                CompletedQueue.Clear();
                LoadQueueData();
            }
        }

        #endregion

        #region Private Methods

        private async void LoadQueueData()
        {
            try
            {
                IsLoading = true;
                
                // TODO: 调用API获取排队数据
                await Task.Delay(1000); // 模拟API调用
                
                // 临时测试数据
                WaitingQueue.Clear();
                ProcessingQueue.Clear();
                CompletedQueue.Clear();
                
                // 等待队列
                for (int i = 1; i <= 5; i++)
                {
                    WaitingQueue.Add(new
                    {
                        Id = i,
                        QueueNumber = $"A{i:D3}",
                        PatientName = $"患者{i}",
                        /* Department = "内科", */
                        DoctorName = $"医生{i}",
                        WaitTime = $"{i * 10}分钟",
                        Status = "等待中"
                    });
                }
                
                // 就诊中队列
                ProcessingQueue.Add(new
                {
                    Id = 6,
                    QueueNumber = "A006",
                    PatientName = "患者6",
                    /* Department = "内科", */
                    DoctorName = "医生6",
                    WaitTime = "0分钟",
                    Status = "就诊中"
                });
                
                // 已完成队列
                for (int i = 7; i <= 10; i++)
                {
                    CompletedQueue.Add(new
                    {
                        Id = i,
                        QueueNumber = $"A{i:D3}",
                        PatientName = $"患者{i}",
                        /* Department = "内科", */
                        DoctorName = $"医生{i}",
                        WaitTime = "已完成",
                        Status = "已完成"
                    });
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载排队数据失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}