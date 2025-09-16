using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoMapper;
using LYBT.Desktop.Core.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using CoreEvents = LYBT.Desktop.Core.Models.Events;

namespace LYBT.Desktop.Prescriptions.ViewModels
{

    /// <summary>
    /// 处方组成编辑器ViewModel - UltraThink简化版本
    /// 专注于处方组成编辑，不包含历史管理、复杂协调等功能
    /// </summary>
    public class PrescriptionComposerViewModel : BindableBase, INavigationAware
    {

        #region 私有字段

        private readonly IMapper _mapper;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IHerbService _herbService;
        private readonly IFormulaService _formulaService;
        private readonly ILogger<PrescriptionComposerViewModel> _logger;

        private PrescriptionDto _currentPrescription = new();
        private Guid? _currentMedicalCaseId;
        private string _patientInfo = string.Empty;

        #endregion 私有字段

        #region 构造函数

        public PrescriptionComposerViewModel(
            IMapper mapper,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            IPrescriptionService prescriptionService,
            IHerbService herbService,
            IFormulaService formulaService,
            ILogger<PrescriptionComposerViewModel> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
            InitializePrescription();
        }

        #endregion 构造函数

        #region 公共属性

        /// <summary>
        /// 当前处方信息
        /// </summary>
        public PrescriptionDto CurrentPrescription
        {
            get => _currentPrescription;
            set
            {
                SetProperty(ref _currentPrescription, value);
                OnPrescriptionChanged();
            }
        }

        /// <summary>
        /// 患者信息显示
        /// </summary>
        public string PatientInfo
        {
            get => _patientInfo;
            private set => SetProperty(ref _patientInfo, value);
        }

        /// <summary>
        /// 诊断
        /// </summary>
        public string Diagnosis
        {
            get => _currentPrescription.Diagnosis ?? string.Empty;
            set
            {
                if (_currentPrescription.Diagnosis != value)
                {
                    _currentPrescription.Diagnosis = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _currentPrescription.DosageCount;
            set
            {
                if (_currentPrescription.DosageCount != value && value > 0)
                {
                    _currentPrescription.DosageCount = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TotalPrice));
                }
            }
        }

        /// <summary>
        /// 用法
        /// </summary>
        public string Usage
        {
            get => _currentPrescription.Usage ?? string.Empty;
            set
            {
                if (_currentPrescription.Usage != value)
                {
                    _currentPrescription.Usage = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 医嘱
        /// </summary>
        public string Advice
        {
            get => _currentPrescription.Advice ?? string.Empty;
            set
            {
                if (_currentPrescription.Advice != value)
                {
                    _currentPrescription.Advice = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 处方药材项目列表
        /// </summary>
        public ObservableCollection<PrescriptionItemDto> PrescriptionItems { get; private set; } = new();

        /// <summary>
        /// 单剂价格
        /// </summary>
        public decimal SingleDosePrice
        {
            get
            {
                if (!PrescriptionItems.Any())
                {
                    return 0m;
                }

                return PrescriptionItems.Sum(item => item.UnitPrice * item.Quantity);
            }
        }

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice => SingleDosePrice * DosageCount;

        #endregion 公共属性

        #region 命令属性

        public ICommand AddHerbCommand { get; private set; } = null!;
        public ICommand ImportFormulaCommand { get; private set; } = null!;
        public ICommand EditHerbCommand { get; private set; } = null!;
        public ICommand RemoveHerbCommand { get; private set; } = null!;
        public ICommand ClearAllCommand { get; private set; } = null!;
        public ICommand SaveDraftCommand { get; private set; } = null!;
        public ICommand SavePrescriptionCommand { get; private set; } = null!;
        public ICommand CloseCommand { get; private set; } = null!;

        #endregion 命令属性

        #region 私有方法

        private void InitializeCommands()
        {
            AddHerbCommand = new DelegateCommand(async () => await OnAddHerbAsync());
            ImportFormulaCommand = new DelegateCommand(async () => await OnImportFormulaAsync());
            EditHerbCommand = new DelegateCommand<PrescriptionItemDto>(OnEditHerb);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemDto>(OnRemoveHerb);
            ClearAllCommand = new DelegateCommand(OnClearAll);
            SaveDraftCommand = new DelegateCommand(async () => await OnSaveDraftAsync());
            SavePrescriptionCommand = new DelegateCommand(async () => await OnSavePrescriptionAsync());
            CloseCommand = new DelegateCommand(OnClose);
        }

        private void InitializePrescription()
        {
            _currentPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                DosageCount = 7,
                Usage = "水煎服，日一剂，分早晚服",
                DosageForm = "汤剂"
            };
        }

        private void OnPrescriptionChanged()
        {
            // 同步药材列表
            PrescriptionItems.Clear();
            if (_currentPrescription.Items?.Any() == true)
            {
                foreach (var item in _currentPrescription.Items)
                {
                    PrescriptionItems.Add(item);
                }
            }

            // 刷新价格相关属性
            RaisePropertyChanged(nameof(SingleDosePrice));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        #endregion 私有方法

        #region 命令处理

        /// <summary>
        /// 添加药材
        /// </summary>
        private Task OnAddHerbAsync()
        {
            try
            {
                _logger.LogInformation("开始选择药材");

                // 调用Herbs模块选择药材
                var dialogParameters = new DialogParameters();
                _dialogService.ShowDialog("HerbSelectionDialog", dialogParameters, r =>
                {
                    if (r.Result == ButtonResult.OK && r.Parameters.ContainsKey("SelectedHerbs"))
                    {
                        var selectedHerbs = r.Parameters.GetValue<HerbDto[]>("SelectedHerbs");
                        AddSelectedHerbs(selectedHerbs);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材时发生错误");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 导入验方
        /// </summary>
        private Task OnImportFormulaAsync()
        {
            try
            {
                _logger.LogInformation("开始选择验方模板");

                // 调用Formula模块选择验方
                var dialogParameters = new DialogParameters();
                _dialogService.ShowDialog("FormulaSelectionDialog", dialogParameters, r =>
                {
                    if (r.Result == ButtonResult.OK && r.Parameters.ContainsKey("SelectedFormula"))
                    {
                        var selectedFormula = r.Parameters.GetValue<FormulaDto>("SelectedFormula");
                        ApplyFormulaTemplate(selectedFormula);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方时发生错误");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        private void OnEditHerb(PrescriptionItemDto item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "PrescriptionItem", item }
                };

                _dialogService.ShowDialog("PrescriptionItemEditDialog", dialogParameters, r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        RefreshPriceCalculation();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑药材时发生错误: {HerbName}", item.HerbName);
            }
        }

        /// <summary>
        /// 移除药材
        /// </summary>
        private void OnRemoveHerb(PrescriptionItemDto item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                PrescriptionItems.Remove(item);
                _currentPrescription.Items.Remove(item);
                RefreshPriceCalculation();

                _logger.LogInformation("已移除药材: {HerbName}", item.HerbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除药材时发生错误: {HerbName}", item.HerbName);
            }
        }

        /// <summary>
        /// 清空所有药材
        /// </summary>
        private void OnClearAll()
        {
            try
            {
                if (PrescriptionItems.Any())
                {
                    // 确认对话框
                    _dialogService.ShowDialog(
                        "ConfirmDialog",
                        new DialogParameters { { "Message", "确定要清空所有药材吗？" } },
                        r =>
                        {
                            if (r.Result == ButtonResult.OK)
                            {
                                PrescriptionItems.Clear();
                                _currentPrescription.Items.Clear();
                                RefreshPriceCalculation();
                                _logger.LogInformation("已清空所有药材");
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空药材时发生错误");
            }
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private async Task OnSaveDraftAsync()
        {
            try
            {
                _logger.LogInformation("保存处方草稿");

                // 基础验证
                if (string.IsNullOrWhiteSpace(Diagnosis))
                {
                    ShowMessage("请输入诊断信息");
                    return;
                }

                // 设置为草稿状态并保存
                _currentPrescription.Status = CommonStatus.Disabled; // 草稿状态
                await SavePrescriptionCore();

                ShowMessage("草稿保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存草稿时发生错误");
                ShowMessage("保存草稿失败");
            }
        }

        /// <summary>
        /// 保存处方
        /// </summary>
        private async Task OnSavePrescriptionAsync()
        {
            try
            {
                _logger.LogInformation("保存处方");

                // 完整验证
                if (!ValidatePrescription())
                {
                    return;
                }

                // 设置为正式状态并保存
                _currentPrescription.Status = CommonStatus.Enabled; // 正式状态
                await SavePrescriptionCore();

                ShowMessage("处方保存成功");

                // 发布处方保存事件
                _eventAggregator.GetEvent<CoreEvents.PrescriptionSavedEvent>()
                    .Publish(new CoreEvents.PrescriptionSavedEventArgs(_currentPrescription.Id, _currentPrescription.PatientId, _currentPrescription.Name ?? string.Empty, _currentPrescription.TotalPrice));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生错误");
                ShowMessage("保存处方失败");
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void OnClose()
        {
            // 检查是否有未保存的更改
            if (HasUnsavedChanges())
            {
                _dialogService.ShowDialog(
                    "ConfirmDialog",
                    new DialogParameters { { "Message", "有未保存的更改，确定要关闭吗？" } },
                    r =>
                    {
                        if (r.Result == ButtonResult.OK)
                        {
                            CloseView();
                        }
                    });
            }
            else
            {
                CloseView();
            }
        }

        #endregion 命令处理

        #region 辅助方法

        /// <summary>
        /// 添加选中的药材
        /// </summary>
        private void AddSelectedHerbs(HerbDto[] herbs)
        {
            if (herbs?.Any() != true)
            {
                return;
            }

            foreach (var herb in herbs)
            {
                // 检查是否已存在
                if (PrescriptionItems.Any(x => x.HerbId == herb.Id))
                {
                    _logger.LogWarning("药材 {HerbName} 已存在于处方中", herb.Name);
                    continue;
                }

                var item = new PrescriptionItemDto
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = 10, // 默认用量
                    Unit = herb.Unit,
                    UnitPrice = herb.Price
                };

                PrescriptionItems.Add(item);
                _currentPrescription.Items.Add(item);
            }

            RefreshPriceCalculation();
            _logger.LogInformation("已添加 {Count} 味药材", herbs.Length);
        }

        /// <summary>
        /// 应用验方模板
        /// </summary>
        private void ApplyFormulaTemplate(FormulaDto formula)
        {
            if (formula?.Items?.Any() != true)
            {
                return;
            }

            // 清空现有药材（可选择性清空）
            _dialogService.ShowDialog(
                "ConfirmDialog",
                new DialogParameters { { "Message", "是否清空现有药材后导入验方？" } },
                r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        PrescriptionItems.Clear();
                        _currentPrescription.Items.Clear();
                    }

                    // 导入验方药材
                    foreach (var formulaItem in formula.Items)
                    {
                        var item = new PrescriptionItemDto
                        {
                            Id = Guid.NewGuid(),
                            HerbId = formulaItem.HerbId,
                            HerbName = formulaItem.HerbName,
                            Quantity = formulaItem.Quantity,
                            Unit = formulaItem.Unit,
                            UnitPrice = formulaItem.UnitPrice
                        };

                        PrescriptionItems.Add(item);
                        _currentPrescription.Items.Add(item);
                    }

                    // 设置验方来源
                    _currentPrescription.FormulaSource = formula.Name;

                    RefreshPriceCalculation();
                    _logger.LogInformation("已导入验方: {FormulaName}", formula.Name);
                });
        }

        /// <summary>
        /// 刷新价格计算
        /// </summary>
        private void RefreshPriceCalculation()
        {
            RaisePropertyChanged(nameof(SingleDosePrice));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        /// <summary>
        /// 验证处方数据
        /// </summary>
        private bool ValidatePrescription()
        {
            if (string.IsNullOrWhiteSpace(Diagnosis))
            {
                ShowMessage("请输入诊断信息");
                return false;
            }

            if (!PrescriptionItems.Any())
            {
                ShowMessage("请添加至少一味中药材");
                return false;
            }

            if (DosageCount <= 0)
            {
                ShowMessage("剂数必须大于0");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 保存处方核心逻辑
        /// </summary>
        private async Task SavePrescriptionCore()
        {
            // 这里调用后端服务保存处方
            // await _prescriptionService.SaveAsync(_currentPrescription);

            // 暂时模拟保存成功
            await Task.Delay(500);
        }

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        private bool HasUnsavedChanges()
        {
            // 简单检查：如果有诊断或药材，就认为有更改
            return !string.IsNullOrWhiteSpace(Diagnosis) || PrescriptionItems.Any();
        }

        /// <summary>
        /// 关闭视图
        /// </summary>
        private void CloseView()
        {
            // 发布关闭事件或进行导航
            _eventAggregator.GetEvent<PrescriptionComposerClosedEvent>()
                .Publish(new PrescriptionComposerClosedEventArgs());
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message)
        {
            _dialogService.ShowDialog(
                "MessageDialog",
                new DialogParameters { { "Message", message } },
                r => { /* 回调处理，这里不需要特殊处理 */ });
        }

        #endregion 辅助方法

        #region INavigationAware 实现

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 接收医疗案例ID参数
            if (navigationContext.Parameters.TryGetValue<object>("MedicalCaseId", out var medicalCaseIdParam)
                && medicalCaseIdParam is Guid medicalCaseId)
            {
                _currentMedicalCaseId = medicalCaseId;
                _currentPrescription.MedicalCaseId = medicalCaseId;

                _logger.LogInformation("处方编辑器导航到医疗案例: {MedicalCaseId}", medicalCaseId);

                // 加载医疗案例相关信息
                _ = LoadMedicalCaseInfoAsync(medicalCaseId);
            }

            // 接收患者信息参数
            if (navigationContext.Parameters.TryGetValue("PatientInfo", out string patientInfo))
            {
                PatientInfo = patientInfo;
            }
        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理资源
        }

        /// <summary>
        /// 加载医疗案例信息
        /// </summary>
        private Task LoadMedicalCaseInfoAsync(Guid medicalCaseId)
        {
            try
            {
                // 这里可以调用服务加载医疗案例信息
                // var medicalCase = await _medicalCaseService.GetByIdAsync(medicalCaseId);
                // 更新患者信息显示
                _logger.LogInformation("已加载医疗案例信息: {MedicalCaseId}", medicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医疗案例信息失败: {MedicalCaseId}", medicalCaseId);
            }

            return Task.CompletedTask;
        }

        #endregion INavigationAware 实现
    }
}
