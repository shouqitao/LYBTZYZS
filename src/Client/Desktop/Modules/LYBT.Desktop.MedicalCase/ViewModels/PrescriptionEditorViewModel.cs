using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 8列DataGrid行模型（简化版）
    /// 每行包含4个药材（药材+用量）
    /// </summary>
    public class SimpleItemRow : BindableBase
    {
        private PrescriptionItemDto _item1 = new();
        private PrescriptionItemDto _item2 = new();
        private PrescriptionItemDto _item3 = new();
        private PrescriptionItemDto _item4 = new();

        public PrescriptionItemDto Item1
        {
            get => _item1;
            set => SetProperty(ref _item1, value);
        }

        public PrescriptionItemDto Item2
        {
            get => _item2;
            set => SetProperty(ref _item2, value);
        }

        public PrescriptionItemDto Item3
        {
            get => _item3;
            set => SetProperty(ref _item3, value);
        }

        public PrescriptionItemDto Item4
        {
            get => _item4;
            set => SetProperty(ref _item4, value);
        }
    }
    /// <summary>
    /// 处方编辑器ViewModel - Task #1499 Step 3简化实现
    /// 简化版处方编辑逻辑，不依赖Prescriptions模块（避免循环依赖）
    /// Epic #1494: 医案流程UI重构
    ///
    /// ⚠️ 架构债务：存在循环依赖问题 Prescriptions ↔ MedicalCase
    /// TODO: 创建Issue修复架构问题，将IMedicalCaseRepository移到共享层
    /// </summary>
    public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;

        #endregion

        #region 数据属性

        private PatientDto? _currentPatient;
        private Guid _medicalCaseId;

        /// <summary>
        /// 当前患者信息（从MedicalCaseFlowViewModel传递）
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        /// <summary>
        /// 医疗案例ID（从MedicalCaseFlowViewModel传递）
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        /// <summary>
        /// 处方项行集合（8列DataGrid绑定）
        /// </summary>
        public ObservableCollection<SimpleItemRow> ItemRows { get; } = new();

        private int _dosageCount = 7;
        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    RaisePropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private string _usage = "水煎服，日一剂，早晚分服";
        /// <summary>
        /// 用法
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private string _medicalAdvice = string.Empty;
        /// <summary>
        /// 医嘱
        /// </summary>
        public string MedicalAdvice
        {
            get => _medicalAdvice;
            set => SetProperty(ref _medicalAdvice, value);
        }

        private string _remark = string.Empty;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 单剂价格（自动计算）
        /// </summary>
        public decimal SingleDosagePrice
        {
            get
            {
                // TODO: 从所有非空Items计算总价（需要药材价格数据）
                var allItems = GetAllItems();
                return allItems.Sum(item => item.Dosage * 1.0m); // 临时：假设每克1元
            }
        }

        /// <summary>
        /// 总价格（单剂价格 × 剂数）
        /// </summary>
        public decimal TotalPrice => SingleDosagePrice * DosageCount;

        /// <summary>
        /// 药材总数
        /// </summary>
        public int ItemCount
        {
            get
            {
                var allItems = GetAllItems();
                return allItems.Count;
            }
        }

        /// <summary>
        /// 药材列表（简化版：暂时为空，支持手动输入）
        /// TODO: 集成Herbs模块获取药材数据
        /// </summary>
        public ObservableCollection<object> FilteredHerbs { get; } = new();

        #endregion

        #region 命令

        public DelegateCommand AddRowCommand { get; }

        #endregion

        #region IValidatable实现

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 验证处方数据（至少1个药材）
        /// </summary>
        public bool Validate()
        {
            if (CurrentPatient == null)
            {
                ValidationMessage = "请先选择患者";
                return false;
            }

            if (MedicalCaseId == Guid.Empty)
            {
                ValidationMessage = "MedicalCaseId不能为空";
                return false;
            }

            var allItems = GetAllItems();
            if (allItems.Count == 0)
            {
                ValidationMessage = "请至少添加一个药材";
                return false;
            }

            // 验证每个药材的必填字段
            foreach (var item in allItems)
            {
                if (item.HerbId == Guid.Empty)
                {
                    ValidationMessage = "存在未选择药材的行";
                    return false;
                }

                if (item.Dosage <= 0)
                {
                    ValidationMessage = $"药材 {item.HerbName} 的用量无效";
                    return false;
                }
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存处方 - Task #1499: 创建Prescription并关联到MedicalCase
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存处方...");

                // 1. 验证数据
                if (!Validate())
                {
                    Logger.LogWarning("处方验证失败：{Message}", ValidationMessage);
                    return false;
                }

                // 2. 构造PrescriptionCreateDto
                var allItems = GetAllItems();
                var prescriptionDto = new PrescriptionCreateDto
                {
                    PatientId = CurrentPatient!.Id,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    Quantity = DosageCount, // 剂数
                    Usage = Usage,
                    Advice = MedicalAdvice,
                    Notes = Remark,
                    Items = allItems.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = 1.0m // TODO: 从Herbs模块获取真实价格
                    }).ToList()
                };

                // 3. 调用API创建Prescription
                // TODO: 实现IPrescriptionRepository（当前MedicalCase模块无此依赖）
                Logger.LogInformation("处方数据已准备，等待API集成");
                Logger.LogInformation("处方包含 {ItemCount} 味药材，{DosageCount} 剂，总价 {TotalPrice:F2}元",
                    ItemCount, DosageCount, TotalPrice);

                // 4. 更新MedicalCase关联Prescription
                // TODO: 实现UpdatePrescriptionIdAsync方法

                await ShowSuccessMessageAsync("处方已保存（演示模式）");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方时发生异常");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
                return false;
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 添加8列行（包含4个药材空位）
        /// </summary>
        private void ExecuteAddRow()
        {
            ItemRows.Add(new SimpleItemRow
            {
                Item1 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item2 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item3 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item4 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" }
            });

            RaisePropertyChanged(nameof(ItemCount));
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        /// <summary>
        /// 从ItemRows提取所有非空药材
        /// </summary>
        private List<PrescriptionItemDto> GetAllItems()
        {
            var result = new List<PrescriptionItemDto>();

            foreach (var row in ItemRows)
            {
                if (row.Item1.HerbId != Guid.Empty)
                    result.Add(row.Item1);
                if (row.Item2.HerbId != Guid.Empty)
                    result.Add(row.Item2);
                if (row.Item3.HerbId != Guid.Empty)
                    result.Add(row.Item3);
                if (row.Item4.HerbId != Guid.Empty)
                    result.Add(row.Item4);
            }

            return result;
        }

        #endregion

        #region 构造函数

        public PrescriptionEditorViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化命令
            AddRowCommand = new DelegateCommand(ExecuteAddRow);

            Logger.LogInformation("PrescriptionEditorViewModel已初始化（简化版）");
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // 接收患者信息和MedicalCaseId
                if (navigationContext.Parameters.ContainsKey("Patient"))
                {
                    CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("Patient");
                    Logger.LogInformation("接收到患者信息：{PatientName}", CurrentPatient?.Name);
                }

                if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
                {
                    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                    Logger.LogInformation("接收到MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                }

                // 添加初始行（5行 = 20个药材空位）
                if (ItemRows.Count == 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ExecuteAddRow();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到处方编辑器时发生异常");
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public override void OnNavigatedFrom(NavigationContext navigationContext) { }

        #endregion
    }
}
