using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 创建医疗案例对话框 ViewModel
    /// TODO: 当前使用 Mock 实现，待后续集成真实服务
    /// </summary>
    public class CreateMedicalCaseDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 属性

        private ObservableCollection<PatientItem> _patients = new();
        /// <summary>
        /// 患者列表
        /// </summary>
        public ObservableCollection<PatientItem> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private ObservableCollection<DoctorItem> _doctors = new();
        /// <summary>
        /// 医生列表
        /// </summary>
        public ObservableCollection<DoctorItem> Doctors
        {
            get => _doctors;
            set => SetProperty(ref _doctors, value);
        }

        private MedicalCaseModel _medicalCase = new();
        /// <summary>
        /// 医疗案例数据
        /// </summary>
        public MedicalCaseModel MedicalCase
        {
            get => _medicalCase;
            set => SetProperty(ref _medicalCase, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region IDialogAware 实现

        public string Title => "创建医疗案例";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // TODO: 加载患者和医生列表
            LoadMockData();
        }

        #endregion

        #region 构造函数

        public CreateMedicalCaseDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave)
                .ObservesProperty(() => MedicalCase.PatientId)
                .ObservesProperty(() => MedicalCase.DoctorId);

            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载 Mock 数据
        /// </summary>
        private void LoadMockData()
        {
            // Mock 患者数据
            Patients = new ObservableCollection<PatientItem>
            {
                new PatientItem { Id = Guid.NewGuid(), Name = "张三" },
                new PatientItem { Id = Guid.NewGuid(), Name = "李四" },
                new PatientItem { Id = Guid.NewGuid(), Name = "王五" }
            };

            // Mock 医生数据
            Doctors = new ObservableCollection<DoctorItem>
            {
                new DoctorItem { Id = Guid.NewGuid(), Name = "Dr. Smith" },
                new DoctorItem { Id = Guid.NewGuid(), Name = "Dr. Johnson" },
                new DoctorItem { Id = Guid.NewGuid(), Name = "Dr. Williams" }
            };
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return MedicalCase.PatientId != Guid.Empty &&
                   MedicalCase.DoctorId != Guid.Empty;
        }

        /// <summary>
        /// 保存医疗案例
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存医疗案例...");

                // TODO: 调用真实服务保存数据
                await Task.Delay(500); // Mock 延迟

                await ShowSuccessMessageAsync("医疗案例创建成功");

                var dialogResult = new DialogResult(ButtonResult.OK);
                dialogResult.Parameters.Add("MedicalCase", MedicalCase);

                RequestClose?.Invoke(dialogResult);

                Logger.LogInformation("医疗案例创建成功: PatientId={PatientId}, DoctorId={DoctorId}",
                    MedicalCase.PatientId, MedicalCase.DoctorId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存医疗案例时发生异常");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region 辅助类

        public class PatientItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class DoctorItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class MedicalCaseModel
        {
            public Guid PatientId { get; set; }
            public Guid DoctorId { get; set; }
            public string Remark { get; set; } = string.Empty;
        }

        #endregion
    }
}
