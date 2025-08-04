using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Physiotherapy;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.Physiotherapy.ViewModels
{
    /// <summary>
    /// 理疗管理视图模型
    /// </summary>
    public class PhysiotherapyManagementViewModel : BindableBase
    {
        private readonly IPhysiotherapyService _physiotherapyService;
        #region 属性

        private ObservableCollection<PhysiotherapyAppointmentInfo> _appointmentList;
        public ObservableCollection<PhysiotherapyAppointmentInfo> AppointmentList
        {
            get => _appointmentList;
            set => SetProperty(ref _appointmentList, value);
        }

        private PhysiotherapyAppointmentInfo _selectedAppointment;
        public PhysiotherapyAppointmentInfo SelectedAppointment
        {
            get => _selectedAppointment;
            set => SetProperty(ref _selectedAppointment, value);
        }

        private ObservableCollection<TreatmentTypeInfo> _treatmentTypeList;
        public ObservableCollection<TreatmentTypeInfo> TreatmentTypeList
        {
            get => _treatmentTypeList;
            set => SetProperty(ref _treatmentTypeList, value);
        }

        private TreatmentTypeInfo _selectedTreatmentType;
        public TreatmentTypeInfo SelectedTreatmentType
        {
            get => _selectedTreatmentType;
            set => SetProperty(ref _selectedTreatmentType, value);
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _selectedStatus = "全部状态";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private DateTime? _selectedDate = DateTime.Today;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        private int _pendingCount = 5;
        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        private int _todayTreatmentCount = 12;
        public int TodayTreatmentCount
        {
            get => _todayTreatmentCount;
            set => SetProperty(ref _todayTreatmentCount, value);
        }

        #endregion

        #region 命令

        public DelegateCommand AddAppointmentCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<object> StartTreatmentCommand { get; }
        public DelegateCommand<object> CompleteTreatmentCommand { get; }
        public DelegateCommand<object> EditAppointmentCommand { get; }
        public DelegateCommand<object> CancelAppointmentCommand { get; }
        public DelegateCommand AddTreatmentTypeCommand { get; }
        public DelegateCommand EditTreatmentTypeCommand { get; }
        public DelegateCommand DeleteTreatmentTypeCommand { get; }

        #endregion

        public PhysiotherapyManagementViewModel(IPhysiotherapyService physiotherapyService)
        {
            _physiotherapyService = physiotherapyService;
            
            AppointmentList = new ObservableCollection<PhysiotherapyAppointmentInfo>();
            TreatmentTypeList = new ObservableCollection<TreatmentTypeInfo>();

            // 初始化命令
            AddAppointmentCommand = new DelegateCommand(() => MessageBox.Show("新增预约功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            SearchCommand = new DelegateCommand(async () => await LoadAppointments());
            RefreshCommand = new DelegateCommand(async () => await LoadAppointments());
            StartTreatmentCommand = new DelegateCommand<object>(obj => MessageBox.Show("开始理疗功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            CompleteTreatmentCommand = new DelegateCommand<object>(obj => MessageBox.Show("完成理疗功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            EditAppointmentCommand = new DelegateCommand<object>(obj => MessageBox.Show("编辑预约功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            CancelAppointmentCommand = new DelegateCommand<object>(obj => MessageBox.Show("取消预约功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            AddTreatmentTypeCommand = new DelegateCommand(() => MessageBox.Show("新增项目功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            EditTreatmentTypeCommand = new DelegateCommand(() => MessageBox.Show("编辑项目功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
            DeleteTreatmentTypeCommand = new DelegateCommand(() => MessageBox.Show("删除项目功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information));

            // 加载数据
            _ = LoadAppointments();
            _ = LoadTreatmentTypes();
        }

        private async Task LoadAppointments()
        {
            try
            {
                var appointments = await _physiotherapyService.GetAppointmentsAsync(SelectedDate, SelectedStatus);
                AppointmentList.Clear();
                foreach (var appointment in appointments)
                {
                    AppointmentList.Add(appointment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载预约列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTreatmentTypes()
        {
            try
            {
                var treatmentTypes = await _physiotherapyService.GetTreatmentTypesAsync();
                TreatmentTypeList.Clear();
                foreach (var type in treatmentTypes)
                {
                    TreatmentTypeList.Add(type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载理疗项目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}