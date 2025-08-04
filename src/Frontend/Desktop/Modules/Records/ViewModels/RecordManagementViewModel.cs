using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Base;
using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Commands;

namespace LYBT.WPF.Client.Modules.Records.ViewModels
{
    /// <summary>
    /// 病历管理视图模型
    /// </summary>
    public class RecordManagementViewModel : ViewModelBase
    {
        private readonly IRecordService _recordService;

        #region 属性

        private ObservableCollection<object> _recordList;
        public ObservableCollection<object> RecordList
        {
            get => _recordList;
            set => SetProperty(ref _recordList, value);
        }

        private object _selectedRecord;
        public object SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private object _selectedDoctor;
        public object SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetProperty(ref _selectedDoctor, value);
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private int _totalRecordCount = 1286;
        public int TotalRecordCount
        {
            get => _totalRecordCount;
            set => SetProperty(ref _totalRecordCount, value);
        }

        private int _todayNewCount = 15;
        public int TodayNewCount
        {
            get => _todayNewCount;
            set => SetProperty(ref _todayNewCount, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand<object> ViewRecordCommand { get; }
        public DelegateCommand<object> EditRecordCommand { get; }
        public DelegateCommand<object> PrintRecordCommand { get; }
        public DelegateCommand<object> DeleteRecordCommand { get; }

        #endregion

        public RecordManagementViewModel(IRecordService recordService)
        {
            _recordService = recordService;

            RecordList = new ObservableCollection<object>();

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchRecords());
            RefreshCommand = new DelegateCommand(async () => await LoadRecords());
            ExportCommand = new DelegateCommand(async () => await ExportRecords());
            ViewRecordCommand = new DelegateCommand<object>(ViewRecord);
            EditRecordCommand = new DelegateCommand<object>(EditRecord);
            PrintRecordCommand = new DelegateCommand<object>(PrintRecord);
            DeleteRecordCommand = new DelegateCommand<object>(DeleteRecord);

            // 加载数据
            _ = LoadRecords();
        }

        private async Task LoadRecords()
        {
            try
            {
                SetBusy(true);
                await Task.Delay(300); // 模拟加载
                // TODO: 实现加载病历列表
            }
            catch (Exception ex)
            {
                ShowError($"加载病历列表失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task SearchRecords()
        {
            await LoadRecords();
        }

        private async Task ExportRecords()
        {
            try
            {
                SetBusy(true);
                await Task.Delay(500); // 模拟导出
                ShowInfo("病历导出成功");
            }
            catch (Exception ex)
            {
                ShowError($"导出失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ViewRecord(object record)
        {
            ShowInfo("查看病历功能待实现");
        }

        private void EditRecord(object record)
        {
            ShowInfo("编辑病历功能待实现");
        }

        private void PrintRecord(object record)
        {
            ShowInfo("打印病历功能待实现");
        }

        private void DeleteRecord(object record)
        {
            if (ShowConfirm("确定要删除这份病历吗？"))
            {
                ShowInfo("删除病历功能待实现");
            }
        }
    }
}