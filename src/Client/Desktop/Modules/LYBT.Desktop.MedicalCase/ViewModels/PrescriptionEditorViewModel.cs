using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 处方编辑器ViewModel - Task #1499
    /// 8列DataGrid布局（每行4个药材），支持手工录入、验方导入、历史复制
    ///
    /// 架构说明：
    /// 为避免循环依赖（MedicalCase ↔ Prescriptions），本ViewModel暂不注入Repository
    /// SaveAsync()使用临时Mock实现，待后续通过事件总线或服务抽象层解耦
    /// </summary>
    public class PrescriptionEditorViewModel : UnifiedViewModelBase, ISaveable, IValidatable
    {
        #region 字段

        private readonly ICommonDialogService _dialogService;
        private Guid _medicalCaseId = Guid.Empty;
        private Guid _patientId = Guid.Empty;

        #endregion

        #region 属性

        private ObservableCollection<PrescriptionItemRowViewModel> _itemRows = new();
        /// <summary>
        /// DataGrid行集合（每行4个药材）
        /// </summary>
        public ObservableCollection<PrescriptionItemRowViewModel> ItemRows
        {
            get => _itemRows;
            set => SetProperty(ref _itemRows, value);
        }

        private ObservableCollection<HerbDto> _allHerbs = new();
        /// <summary>
        /// 所有药材列表（用于ComboBox绑定）
        /// </summary>
        public ObservableCollection<HerbDto> AllHerbs
        {
            get => _allHerbs;
            set => SetProperty(ref _allHerbs, value);
        }

        private int _dosageCount = 7;
        /// <summary>
        /// 剂数（默认7帖）
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

        private string _usage = "水煎服，一日一剂";
        /// <summary>
        /// 用法说明
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
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

        private int _selectedTabIndex = 0;
        /// <summary>
        /// 当前选中的Tab索引（0=手工录入, 1=验方导入, 2=历史复制）
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// 单剂价格（计算属性）
        /// </summary>
        public decimal SingleDosagePrice
        {
            get
            {
                decimal total = 0;
                foreach (var row in ItemRows)
                {
                    total += row.GetRowSubtotal();
                }
                return total;
            }
        }

        /// <summary>
        /// 总价格（计算属性）
        /// </summary>
        public decimal TotalPrice => SingleDosagePrice * DosageCount;

        /// <summary>
        /// 药材总数（计算属性）
        /// </summary>
        public int HerbCount
        {
            get
            {
                int count = 0;
                foreach (var row in ItemRows)
                {
                    if (row.Herb1.Herb != null) count++;
                    if (row.Herb2.Herb != null) count++;
                    if (row.Herb3.Herb != null) count++;
                    if (row.Herb4.Herb != null) count++;
                }
                return count;
            }
        }

        #endregion

        #region 命令

        public DelegateCommand AddRowCommand { get; }
        public DelegateCommand<PrescriptionItemRowViewModel> DeleteRowCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand ImportFormulaCommand { get; }
        public DelegateCommand ImportHistoryCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionEditorViewModel(
            ICommonDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            AddRowCommand = new DelegateCommand(ExecuteAddRow);
            DeleteRowCommand = new DelegateCommand<PrescriptionItemRowViewModel>(ExecuteDeleteRow, CanExecuteDeleteRow);
            ClearCommand = new DelegateCommand(ExecuteClear);
            ImportFormulaCommand = new DelegateCommand(async () => await ExecuteImportFormulaAsync());
            ImportHistoryCommand = new DelegateCommand(async () => await ExecuteImportHistoryAsync());

            // 初始化默认行
            AddDefaultRows();

            Logger.LogInformation("PrescriptionEditorViewModel已初始化");
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存处方数据
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                if (_medicalCaseId == Guid.Empty)
                {
                    Logger.LogError("MedicalCaseId未设置，无法保存处方");
                    await ShowErrorMessageAsync("未关联医案，无法保存处方");
                    return false;
                }

                SetIsBusy(true, "正在保存处方...");

                // TODO: Task #1499 - 临时Mock实现
                // 待解决循环依赖后，通过事件总线或服务抽象层调用PrescriptionRepository
                // 构建PrescriptionCreateDto
                var items = GetPrescriptionItems();
                Logger.LogInformation("处方药材数量：{Count}，剂数：{DosageCount}，总价：{TotalPrice:C2}",
                    items.Count, DosageCount, TotalPrice);

                // 临时模拟保存
                await Task.Delay(500);
                Logger.LogInformation("处方保存成功（模拟），MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方失败");
                await ShowErrorMessageAsync($"保存处方失败：{ex.Message}");
                return false;
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region IValidatable实现

        /// <summary>
        /// 验证处方数据
        /// </summary>
        public bool Validate()
        {
            if (DosageCount <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Usage))
            {
                return false;
            }

            // 检查是否至少有一味药材
            int herbCount = GetPrescriptionItems().Count;
            if (herbCount == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证失败消息
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                if (DosageCount <= 0)
                    return "剂数必须大于0";

                if (string.IsNullOrWhiteSpace(Usage))
                    return "用法说明不能为空";

                int herbCount = GetPrescriptionItems().Count;
                if (herbCount == 0)
                    return "处方至少需要一味药材";

                return string.Empty;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置医案ID和患者ID（从MedicalCaseFlowViewModel调用）
        /// </summary>
        public void SetMedicalCaseIdAndPatientId(Guid medicalCaseId, Guid patientId)
        {
            _medicalCaseId = medicalCaseId;
            _patientId = patientId;
            Logger.LogInformation("设置MedicalCaseId: {MedicalCaseId}, PatientId: {PatientId}", medicalCaseId, patientId);
        }

        /// <summary>
        /// 加载所有药材列表（临时Mock数据）
        /// </summary>
        public async Task LoadHerbsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载药材列表...");

                // TODO: Task #1499 - 临时Mock数据
                // 待解决循环依赖后，通过事件总线或服务抽象层调用HerbRepository
                await Task.Delay(300);
                AllHerbs = new ObservableCollection<HerbDto>
                {
                    new HerbDto { Id = Guid.NewGuid(), Name = "人参", PinYinCode = "RS", Unit = "克", Price = 5.0m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Unit = "克", Price = 3.5m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Unit = "克", Price = 4.0m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "川芎", PinYinCode = "CX", Unit = "克", Price = 3.0m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "白术", PinYinCode = "BS", Unit = "克", Price = 2.5m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "茯苓", PinYinCode = "FL", Unit = "克", Price = 2.0m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "GC", Unit = "克", Price = 1.5m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "生姜", PinYinCode = "SJ", Unit = "克", Price = 1.0m }
                };

                Logger.LogInformation("已加载{Count}个药材（Mock数据）", AllHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表失败");
                await ShowErrorMessageAsync($"加载药材列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 添加新行（4个空药材）
        /// </summary>
        private void ExecuteAddRow()
        {
            var newRow = new PrescriptionItemRowViewModel(this);
            ItemRows.Add(newRow);
            RaisePropertyChanged(nameof(HerbCount));
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
            Logger.LogInformation("添加新行，当前行数：{RowCount}", ItemRows.Count);
        }

        /// <summary>
        /// 删除行
        /// </summary>
        private void ExecuteDeleteRow(PrescriptionItemRowViewModel row)
        {
            if (row != null && ItemRows.Contains(row))
            {
                ItemRows.Remove(row);
                RaisePropertyChanged(nameof(HerbCount));
                RaisePropertyChanged(nameof(SingleDosagePrice));
                RaisePropertyChanged(nameof(TotalPrice));
                DeleteRowCommand.RaiseCanExecuteChanged();
                Logger.LogInformation("删除行，当前行数：{RowCount}", ItemRows.Count);
            }
        }

        private bool CanExecuteDeleteRow(PrescriptionItemRowViewModel row)
        {
            return ItemRows.Count > 1; // 至少保留1行
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ExecuteClear()
        {
            ItemRows.Clear();
            AddDefaultRows();
            DosageCount = 7;
            Usage = "水煎服，一日一剂";
            Remark = string.Empty;
            RaisePropertyChanged(nameof(HerbCount));
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
            Logger.LogInformation("清空处方表单");
        }

        /// <summary>
        /// 验方导入
        /// </summary>
        private async Task ExecuteImportFormulaAsync()
        {
            try
            {
                Logger.LogInformation("执行验方导入");
                // TODO: Task #1499 - 实现验方选择对话框
                // 1. 弹出验方搜索对话框
                // 2. 用户选择验方后，调用ImportFormulaIntoPrescriptionAsync
                // 3. 自动填充ItemRows
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "验方导入失败");
                await ShowErrorMessageAsync($"验方导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 历史复制
        /// </summary>
        private async Task ExecuteImportHistoryAsync()
        {
            try
            {
                Logger.LogInformation("执行历史复制");
                // TODO: Task #1499 - 实现历史处方选择对话框
                // 1. 弹出患者历史处方列表对话框
                // 2. 用户选择历史处方后，复制处方项到ItemRows
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "历史复制失败");
                await ShowErrorMessageAsync($"历史复制失败：{ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加默认行（2行，每行4个空药材）
        /// </summary>
        private void AddDefaultRows()
        {
            ItemRows.Add(new PrescriptionItemRowViewModel(this));
            ItemRows.Add(new PrescriptionItemRowViewModel(this));
        }

        /// <summary>
        /// 获取处方项列表（过滤空药材）
        /// </summary>
        private List<PrescriptionItemCreateDto> GetPrescriptionItems()
        {
            var items = new List<PrescriptionItemCreateDto>();

            foreach (var row in ItemRows)
            {
                // Herb1
                if (row.Herb1.Herb != null && row.Herb1.Dosage > 0)
                {
                    items.Add(new PrescriptionItemCreateDto
                    {
                        HerbId = row.Herb1.Herb.Id,
                        Quantity = row.Herb1.Dosage,
                        Unit = row.Herb1.Herb.Unit
                    });
                }

                // Herb2
                if (row.Herb2.Herb != null && row.Herb2.Dosage > 0)
                {
                    items.Add(new PrescriptionItemCreateDto
                    {
                        HerbId = row.Herb2.Herb.Id,
                        Quantity = row.Herb2.Dosage,
                        Unit = row.Herb2.Herb.Unit
                    });
                }

                // Herb3
                if (row.Herb3.Herb != null && row.Herb3.Dosage > 0)
                {
                    items.Add(new PrescriptionItemCreateDto
                    {
                        HerbId = row.Herb3.Herb.Id,
                        Quantity = row.Herb3.Dosage,
                        Unit = row.Herb3.Herb.Unit
                    });
                }

                // Herb4
                if (row.Herb4.Herb != null && row.Herb4.Dosage > 0)
                {
                    items.Add(new PrescriptionItemCreateDto
                    {
                        HerbId = row.Herb4.Herb.Id,
                        Quantity = row.Herb4.Dosage,
                        Unit = row.Herb4.Herb.Unit
                    });
                }
            }

            return items;
        }

        /// <summary>
        /// 通知价格更新（从HerbItemViewModel调用）
        /// </summary>
        internal void NotifyPriceChanged()
        {
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
            RaisePropertyChanged(nameof(HerbCount));
        }

        #endregion
    }

    /// <summary>
    /// DataGrid行ViewModel（每行4个药材）
    /// </summary>
    public class PrescriptionItemRowViewModel : BindableBase
    {
        private readonly PrescriptionEditorViewModel _parentViewModel;

        public HerbItemViewModel Herb1 { get; }
        public HerbItemViewModel Herb2 { get; }
        public HerbItemViewModel Herb3 { get; }
        public HerbItemViewModel Herb4 { get; }

        public PrescriptionItemRowViewModel(PrescriptionEditorViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));

            Herb1 = new HerbItemViewModel(parentViewModel);
            Herb2 = new HerbItemViewModel(parentViewModel);
            Herb3 = new HerbItemViewModel(parentViewModel);
            Herb4 = new HerbItemViewModel(parentViewModel);
        }

        /// <summary>
        /// 获取本行小计（4个药材的总价）
        /// </summary>
        public decimal GetRowSubtotal()
        {
            return Herb1.GetSubtotal() + Herb2.GetSubtotal() + Herb3.GetSubtotal() + Herb4.GetSubtotal();
        }
    }

    /// <summary>
    /// 单个药材项ViewModel
    /// </summary>
    public class HerbItemViewModel : BindableBase
    {
        private readonly PrescriptionEditorViewModel _parentViewModel;

        private HerbDto? _herb;
        /// <summary>
        /// 选中的药材
        /// </summary>
        public HerbDto? Herb
        {
            get => _herb;
            set
            {
                if (SetProperty(ref _herb, value))
                {
                    _parentViewModel.NotifyPriceChanged();
                }
            }
        }

        private decimal _dosage = 0;
        /// <summary>
        /// 用量（克/个/片等）
        /// </summary>
        public decimal Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    _parentViewModel.NotifyPriceChanged();
                }
            }
        }

        public HerbItemViewModel(PrescriptionEditorViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
        }

        /// <summary>
        /// 计算小计（单价 × 用量）
        /// </summary>
        public decimal GetSubtotal()
        {
            if (Herb == null || Dosage <= 0)
                return 0;

            return Herb.Price * Dosage;
        }
    }
}
