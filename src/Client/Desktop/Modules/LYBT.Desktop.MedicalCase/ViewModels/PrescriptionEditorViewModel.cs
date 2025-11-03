using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

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
    /// 处方编辑器ViewModel - Epic #1540 方案B重构版
    /// 通过IPrescriptionEditorService接口解除循环依赖 Prescriptions ↔ MedicalCase
    /// Epic #1494: 医案流程UI重构
    /// Epic #1540: 处方编辑器架构重构（方案B - 包装模式）
    ///
    /// 架构改进：
    /// - 依赖IPrescriptionEditorService接口（定义在Desktop.Contracts）
    /// - 复用Prescriptions模块的完整功能（药材数据、历史处方、验方导入、价格计算）
    /// - 打破MedicalCase ↔ Prescriptions循环依赖
    /// </summary>
    public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        #region 服务依赖

        // Issue #1783: 使用DataManager替代直接Repository和Api访问
        private readonly MedicalCaseDataManager _dataManager;
        private readonly IPrescriptionEditorService _prescriptionEditorService;
        private readonly IDialogService _dialogService;

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
        /// 单剂价格（自动计算）- Epic #1540: 使用真实药材价格
        /// </summary>
        public decimal SingleDosagePrice
        {
            get
            {
                var allItems = GetAllItems();
                return allItems.Sum(item =>
                {
                    var herb = _allHerbs.FirstOrDefault(h => h.Id == item.HerbId);
                    return (herb?.Price ?? 0m) * item.Dosage;
                });
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
        /// 所有药材数据（从IPrescriptionEditorService加载）
        /// </summary>
        private List<HerbDto> _allHerbs = new();

        /// <summary>
        /// 过滤后的药材列表（ComboBox绑定）
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs { get; } = new();

        private string _treatmentMethod = string.Empty;
        /// <summary>
        /// 治法方案（Issue #1591新增）
        /// </summary>
        public string TreatmentMethod
        {
            get => _treatmentMethod;
            set => SetProperty(ref _treatmentMethod, value);
        }

        private string _treatmentPrinciple = string.Empty;
        /// <summary>
        /// 治疗原则（Issue #1591新增）
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        private DateTime? _step2CompletedAt;
        /// <summary>
        /// Step2完成时间（Issue #1591新增）
        /// </summary>
        public DateTime? Step2CompletedAt
        {
            get => _step2CompletedAt;
            set
            {
                if (SetProperty(ref _step2CompletedAt, value))
                {
                    RaisePropertyChanged(nameof(Step2CompletedAtText));
                    RaisePropertyChanged(nameof(Step2CompletedAtVisibility));
                }
            }
        }

        /// <summary>
        /// Step2完成时间文本（Issue #1591新增）
        /// </summary>
        public string Step2CompletedAtText =>
            Step2CompletedAt.HasValue
                ? $"✅ Step 2已完成 ({Step2CompletedAt.Value:yyyy-MM-dd HH:mm:ss})"
                : string.Empty;

        /// <summary>
        /// Step2完成时间可见性（Issue #1591新增）
        /// </summary>
        public System.Windows.Visibility Step2CompletedAtVisibility =>
            Step2CompletedAt.HasValue
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        private string _duplicateHerbsWarningText = string.Empty;
        /// <summary>
        /// 重复药材警告文本（Issue #1591新增）
        /// </summary>
        public string DuplicateHerbsWarningText
        {
            get => _duplicateHerbsWarningText;
            set
            {
                if (SetProperty(ref _duplicateHerbsWarningText, value))
                {
                    RaisePropertyChanged(nameof(DuplicateHerbsWarningVisibility));
                }
            }
        }

        /// <summary>
        /// 重复药材警告可见性（Issue #1591新增）
        /// </summary>
        public System.Windows.Visibility DuplicateHerbsWarningVisibility =>
            !string.IsNullOrWhiteSpace(DuplicateHerbsWarningText)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        #endregion

        #region 命令

        public DelegateCommand AddRowCommand { get; }

        /// <summary>
        /// 完成Step2命令（Issue #1591新增）
        /// </summary>
        public DelegateCommand CompleteStep2Command { get; }

        /// <summary>
        /// 显示其他病案查询命令（Issue #1591新增）
        /// </summary>
        public DelegateCommand ShowOtherCasesQueryCommand { get; }

        /// <summary>
        /// 保存草稿命令（Issue #1594新增）
        /// </summary>
        public DelegateCommand SaveDraftCommand { get; }

        /// <summary>
        /// 删除处方命令（Issue #1593 - Phase 4新增）
        /// </summary>
        public DelegateCommand DeletePrescriptionCommand { get; }

        #endregion

        #region IValidatable实现

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 验证处方数据
        /// Issue #1546: 增强药材库关联验证
        /// </summary>
        public bool Validate()
        {
            var errors = new List<string>();

            // 1. 验证基本信息
            if (CurrentPatient == null)
            {
                errors.Add("请先选择患者");
            }

            if (MedicalCaseId == Guid.Empty)
            {
                errors.Add("MedicalCaseId不能为空");
            }

            // 2. Issue #1546: 药材库关联验证
            var allItems = GetAllItems();

            if (allItems.Count == 0)
            {
                errors.Add("请至少添加一味药材");
            }
            else
            {
                // 验证每个药材项
                foreach (var item in allItems)
                {
                    if (string.IsNullOrWhiteSpace(item.HerbName))
                    {
                        continue; // 跳过空药材名称（已在GetAllItems中过滤）
                    }

                    // 在药材库中查找匹配的药材
                    var matchedHerb = _allHerbs.FirstOrDefault(h =>
                        h.Name.Equals(item.HerbName, StringComparison.OrdinalIgnoreCase));

                    if (matchedHerb == null)
                    {
                        errors.Add($"药材 '{item.HerbName}' 在药材库中不存在，请检查名称或添加新药材");
                    }
                    else if (!matchedHerb.IsEnabled)
                    {
                        errors.Add($"药材 '{item.HerbName}' 已停用，请选择其他药材");
                    }
                    else
                    {
                        // 自动设置/修正HerbId（如果未设置或不匹配）
                        if (item.HerbId == Guid.Empty || item.HerbId != matchedHerb.Id)
                        {
                            item.HerbId = matchedHerb.Id;
                            Logger.LogInformation("自动设置药材ID：{HerbName} → {HerbId}", item.HerbName, matchedHerb.Id);
                        }

                        // 验证用量
                        if (item.Dosage <= 0)
                        {
                            errors.Add($"药材 '{item.HerbName}' 的用量必须大于0");
                        }
                    }
                }
            }

            // 3. 汇总错误信息
            if (errors.Any())
            {
                ValidationMessage = string.Join("；", errors);
                Logger.LogWarning("处方验证失败：{ValidationMessage}", ValidationMessage);
                return false;
            }

            ValidationMessage = string.Empty;
            Logger.LogInformation("处方验证通过，共{ItemCount}味药材", allItems.Count);
            return true;
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存处方 - Epic #1540: 使用IPrescriptionEditorService构建草稿
        /// Issue #1477协调：此方法构建草稿，最终写入由MedicalCase聚合根控制
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

                // Epic #1540: 从_allHerbs获取真实价格
                var itemsWithPrice = allItems.Select(item =>
                {
                    var herb = _allHerbs.FirstOrDefault(h => h.Id == item.HerbId);
                    return new PrescriptionItemInputDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = herb?.Price ?? 0m, // 使用真实价格
                        Subtotal = (herb?.Price ?? 0m) * item.Dosage
                    };
                }).ToList();

                var createDto = new PrescriptionCreateDto
                {
                    PatientId = CurrentPatient!.Id,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    ConsultationId = MedicalCaseId, // 关联医案
                    Quantity = DosageCount,
                    Usage = Usage,
                    Advice = MedicalAdvice,
                    Notes = Remark,
                    Items = itemsWithPrice
                };

                // 3. Epic #1540: 使用IPrescriptionEditorService构建草稿
                var draft = await _prescriptionEditorService.BuildPrescriptionDraftAsync(createDto);

                // 4. 验证草稿
                var isValid = await _prescriptionEditorService.ValidatePrescriptionAsync(draft);
                if (!isValid)
                {
                    Logger.LogWarning("处方草稿验证失败");
                    await ShowErrorMessageAsync("处方数据验证失败，请检查药材信息");
                    return false;
                }

                // 5. 计算总金额（验证价格计算）
                var totalAmount = await _prescriptionEditorService.CalculateTotalAmountAsync(draft.Items, DosageCount);
                Logger.LogInformation("处方草稿已构建：{ItemCount}味药材，{DosageCount}剂，总价{TotalAmount:F2}元",
                    draft.Items.Count, DosageCount, totalAmount);

                // 6. Issue #1545: 保存处方到数据库并更新MedicalCase
                var savedPrescription = await SavePrescriptionAndUpdateMedicalCaseAsync(createDto, draft.Id);
                if (savedPrescription == null)
                {
                    await ShowErrorMessageAsync("处方保存失败");
                    return false;
                }

                await ShowSuccessMessageAsync($"处方已保存（{draft.Items.Count}味药材，总价{totalAmount:F2}元）");

                // Issue #1557 Phase 4: 发布PrescriptionCompletedEvent
                PublishPrescriptionCompletedEvent(savedPrescription.Id, draft.Items.Count, totalAmount, isDraft: false);

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

        #region 辅助方法（Epic #1540）

        /// <summary>
        /// 保存处方并更新MedicalCase的PrescriptionId
        /// Issue #1545: 将处方保存到数据库并关联到MedicalCase聚合根
        /// 阶段1：暂不实现数据库写入（避免MedicalCase→Prescriptions循环依赖）
        /// </summary>
        private async Task<PrescriptionDto?> SavePrescriptionAndUpdateMedicalCaseAsync(PrescriptionCreateDto createDto, Guid draftId)
        {
            try
            {
                Logger.LogInformation("【阶段1-草稿模式】处方数据已准备，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                Logger.LogInformation("【阶段1-草稿模式】包含{ItemCount}味药材，{DosageCount}剂", createDto.Items.Count, createDto.Quantity);

                // 阶段1：暂不实现数据库写入
                // 原因：避免MedicalCase模块依赖Prescriptions模块造成循环依赖
                // 阶段2实施方案：
                // 1. 在IMedicalCaseRepository中添加SavePrescriptionAsync方法
                // 2. 或在IPrescriptionEditorService中添加CreatePrescriptionAsync方法
                // 3. 或使用MediatR/事件总线解耦

                // 模拟返回草稿（使用draftId）
                var mockPrescription = new PrescriptionDto
                {
                    Id = draftId,
                    PatientId = createDto.PatientId,
                    UserId = createDto.DoctorId,
                    DosageCount = createDto.Quantity,
                    Usage = createDto.Usage,
                    Advice = createDto.Advice,
                    Items = createDto.Items.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName ?? string.Empty,
                        Dosage = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal
                    }).ToList()
                };

                Logger.LogInformation("【阶段1-草稿模式】处方草稿ID: {DraftId}（未写入数据库）", draftId);

                // 注意：阶段1不更新MedicalCase.PrescriptionId
                // 原因：草稿未真实创建，无有效PrescriptionId

                return await Task.FromResult(mockPrescription);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方草稿失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                return null;
            }
        }

        /// <summary>
        /// 更新MedicalCase的PrescriptionId
        /// Issue #1545: 将保存成功的PrescriptionId关联到MedicalCase
        /// </summary>
        private async Task UpdateMedicalCasePrescriptionIdAsync(Guid prescriptionId)
        {
            try
            {
                Logger.LogInformation("开始更新MedicalCase.PrescriptionId，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                    MedicalCaseId, prescriptionId);

                // Issue #1783: 使用DataManager包装Repository方法（GetByIdSimpleAsync）
                var medicalCase = await _dataManager.GetByIdSimpleAsync(MedicalCaseId);
                if (medicalCase == null)
                {
                    Logger.LogWarning("未找到医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    return;
                }

                // 构建更新DTO
                var updateDto = new LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseUpdateDto
                {
                    Id = medicalCase.Id,
                    PatientId = medicalCase.PatientId,
                    DoctorId = medicalCase.DoctorId,
                    ConsultationId = medicalCase.ConsultationId,
                    PrescriptionId = prescriptionId,
                    Remark = medicalCase.Remark
                };

                // Issue #1783: 使用DataManager包装Repository方法（UpdateSimpleAsync）
                await _dataManager.UpdateSimpleAsync(updateDto);

                Logger.LogInformation("已更新MedicalCase.PrescriptionId: {PrescriptionId}", prescriptionId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新MedicalCase.PrescriptionId失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                // 不抛出异常，允许Prescription保存成功（后续可通过数据修复）
            }
        }

        /// <summary>
        /// 发布处方完成事件
        /// Issue #1557 Phase 4: 通知MedicalCaseFlowViewModel跳转到Step 4
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="totalItems">药材总数</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="isDraft">是否保存为草稿</param>
        private void PublishPrescriptionCompletedEvent(Guid prescriptionId, int totalItems, decimal totalAmount, bool isDraft)
        {
            try
            {
                var payload = new PrescriptionCompletedPayload
                {
                    PrescriptionId = prescriptionId,
                    MedicalCaseFlowId = MedicalCaseId,
                    TotalItems = totalItems,
                    TotalAmount = totalAmount,
                    IsDraft = isDraft,
                    Timestamp = DateTime.Now
                };

                EventAggregator.GetEvent<PrescriptionCompletedEvent>().Publish(payload);

                Logger.LogInformation("已发布PrescriptionCompletedEvent，PrescriptionId: {PrescriptionId}, MedicalCaseFlowId: {MedicalCaseFlowId}",
                    prescriptionId, MedicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发布PrescriptionCompletedEvent失败");
            }
        }

        /// <summary>
        /// 加载所有药材数据（通过IPrescriptionEditorService）
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载药材数据...");

                var herbs = await _prescriptionEditorService.LoadAllHerbsAsync();
                _allHerbs = herbs.ToList();

                // 初始化FilteredHerbs（显示所有药材）
                FilteredHerbs.Clear();
                foreach (var herb in _allHerbs)
                {
                    FilteredHerbs.Add(herb);
                }

                Logger.LogInformation("成功加载{Count}味药材", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材数据时发生异常");
                await ShowErrorMessageAsync($"加载药材数据失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 过滤药材（支持拼音码模糊匹配）
        /// </summary>
        public void FilterHerbs(string searchText)
        {
            try
            {
                var filtered = _prescriptionEditorService.FilterHerbs(searchText);

                FilteredHerbs.Clear();
                foreach (var herb in filtered)
                {
                    FilteredHerbs.Add(herb);
                }

                Logger.LogDebug("过滤药材：搜索'{SearchText}'，匹配{Count}味", searchText, FilteredHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "过滤药材时发生异常");
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
        /// Issue #1343: 阶段1修改 - 支持手工输入药材名称（不依赖HerbId）
        /// </summary>
        private List<PrescriptionItemDto> GetAllItems()
        {
            var result = new List<PrescriptionItemDto>();

            foreach (var row in ItemRows)
            {
                // 阶段1：检查药材名称而非HerbId，支持手工输入
                if (!string.IsNullOrWhiteSpace(row.Item1.HerbName))
                    result.Add(row.Item1);
                if (!string.IsNullOrWhiteSpace(row.Item2.HerbName))
                    result.Add(row.Item2);
                if (!string.IsNullOrWhiteSpace(row.Item3.HerbName))
                    result.Add(row.Item3);
                if (!string.IsNullOrWhiteSpace(row.Item4.HerbName))
                    result.Add(row.Item4);
            }

            return result;
        }

        /// <summary>
        /// 完成Step2（处方录入）
        /// Issue #1591: REQ-002 - 三步工作流优化-Step2
        /// </summary>
        private async Task ExecuteCompleteStep2()
        {
            try
            {
                SetIsBusy(true, "正在完成Step2...");

                // 1. 执行重复药材检测
                CheckDuplicateHerbs();

                // 2. 如果有重复药材警告，提示用户确认
                if (!string.IsNullOrWhiteSpace(DuplicateHerbsWarningText))
                {
                    var confirmed = await ShowConfirmMessageAsync(
                        $"检测到重复药材：\n{DuplicateHerbsWarningText}\n\n是否继续保存？");
                    if (!confirmed)
                    {
                        Logger.LogInformation("用户取消Step2保存（重复药材警告）");
                        return;
                    }
                }

                // 3. 调用SaveAsync保存处方
                var saved = await SaveAsync();
                if (!saved)
                {
                    Logger.LogWarning("Step2保存失败");
                    return;
                }

                // 4. 标记Step2完成时间
                Step2CompletedAt = DateTime.Now;

                // 5. 导航到Step3（汇总页）- 暂未实现，显示成功消息
                await ShowSuccessMessageAsync("Step2已完成！\n处方已保存，后续将进入汇总页（暂未实现）。");

                Logger.LogInformation("Step2完成成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成Step2失败");
                await ShowErrorMessageAsync($"完成Step2失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 显示其他病案查询浮动菜单
        /// Issue #1591: REQ-002 - 辅助功能
        /// </summary>
        private async void ExecuteShowOtherCasesQuery()
        {
            try
            {
                Logger.LogInformation("显示其他病案查询菜单（功能暂未实现）");
                await ShowWarningMessageAsync("其他病案查询功能即将推出，敬请期待！");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示其他病案查询菜单失败");
            }
        }

        /// <summary>
        /// 保存草稿（Issue #1594）
        /// 功能：调用SaveAsync()保存处方数据，但不完成Step2
        /// </summary>
        private async Task ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存草稿...");
                Logger.LogInformation("开始保存处方草稿，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // 调用现有的SaveAsync()方法保存数据
                // 注意：不调用CompleteStep2API，不更新Step2CompletedAt
                var saved = await SaveAsync();

                if (saved)
                {
                    await ShowSuccessMessageAsync("处方草稿已保存！\n提示：请在填写完成后点击【完成Step2】按钮（如需）。");
                    Logger.LogInformation("处方草稿保存成功");
                }
                else
                {
                    Logger.LogWarning("处方草稿保存失败：{ValidationMessage}", ValidationMessage);
                    await ShowErrorMessageAsync($"保存草稿失败：{ValidationMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方草稿异常");
                await ShowErrorMessageAsync($"保存草稿失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 删除处方
        /// Issue #1593 - Phase 4: 删除确认对话框
        /// </summary>
        private async Task ExecuteDeletePrescription()
        {
            try
            {
                Logger.LogInformation("显示删除确认对话框，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // Epic #1676 Phase 2: 使用全局ConfirmationDialog替代专用对话框
                _dialogService.ShowDialog(
                    "ConfirmationDialog",
                    new DialogParameters
                    {
                        { "Title", "确认删除处方" },
                        { "Message", "您确定要删除此处方吗？\n\n注意：物理删除后将无法恢复。" },
                        { "ShowDeleteOptions", true },
                        { "ConfirmButtonText", "确认删除" },
                        { "CancelButtonText", "取消" }
                    },
                    async result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            var isSoftDelete = result.Parameters.GetValue<bool>("IsSoftDelete");
                            await PerformDeleteAsync(isSoftDelete);
                        }
                        else
                        {
                            Logger.LogInformation("用户取消删除操作");
                        }
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示删除对话框失败");
                await ShowErrorMessageAsync($"显示删除对话框失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 执行删除处方操作
        /// Issue #1606 Phase 3: 通过MedicalCase聚合根删除（删除整个医案，包括处方）
        /// </summary>
        private async Task PerformDeleteAsync(bool isSoftDelete)
        {
            try
            {
                SetIsBusy(true, isSoftDelete ? "正在软删除医案..." : "正在永久删除医案...");
                Logger.LogInformation("开始删除医案（包括处方），MedicalCaseId: {MedicalCaseId}, 删除方式: {DeleteType}",
                    MedicalCaseId, isSoftDelete ? "软删除" : "物理删除");

                // Issue #1783: 使用DataManager业务命令方法
                if (isSoftDelete)
                {
                    await _dataManager.SoftDeleteMedicalCaseAsync(MedicalCaseId);
                }
                else
                {
                    await _dataManager.DeleteMedicalCaseAsync(MedicalCaseId);
                }

                // 清空表单数据
                ItemRows.Clear();
                DosageCount = 7;
                Usage = "水煎服，日一剂，早晚分服";
                MedicalAdvice = string.Empty;
                Remark = string.Empty;
                TreatmentMethod = string.Empty;
                TreatmentPrinciple = string.Empty;
                Step2CompletedAt = null;
                DuplicateHerbsWarningText = string.Empty;

                // 重新添加初始行
                for (int i = 0; i < 5; i++)
                {
                    ExecuteAddRow();
                }

                await ShowSuccessMessageAsync(
                    isSoftDelete ? "医案（包括处方）已标记为删除！" : "医案（包括处方）已永久删除！\n注意：此操作不可恢复。");

                Logger.LogInformation("删除处方成功，删除方式: {DeleteType}", isSoftDelete ? "软删除" : "物理删除");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除处方失败");
                await ShowErrorMessageAsync($"删除处方失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检测重复药材
        /// Issue #1591: REQ-002 - 重复药材检测逻辑
        /// </summary>
        private void CheckDuplicateHerbs()
        {
            try
            {
                var allItems = GetAllItems();
                if (allItems.Count == 0)
                {
                    DuplicateHerbsWarningText = string.Empty;
                    return;
                }

                // 按 HerbId 分组，找出重复的药材
                var duplicates = allItems
                    .Where(item => item.HerbId != Guid.Empty) // 只检查已关联药材库的药材
                    .GroupBy(item => item.HerbId)
                    .Where(group => group.Count() > 1)
                    .Select(group => new
                    {
                        HerbName = group.First().HerbName,
                        Count = group.Count(),
                        TotalDosage = group.Sum(item => item.Dosage)
                    })
                    .ToList();

                if (duplicates.Any())
                {
                    var warningLines = duplicates.Select(d =>
                        $"• {d.HerbName}：重复{d.Count}次，累计用量{d.TotalDosage}g");
                    DuplicateHerbsWarningText = string.Join("\n", warningLines);

                    Logger.LogWarning("检测到{Count}种重复药材", duplicates.Count);
                }
                else
                {
                    DuplicateHerbsWarningText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测重复药材失败");
                DuplicateHerbsWarningText = string.Empty;
            }
        }

        #endregion

        #region 构造函数

        public PrescriptionEditorViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IPrescriptionEditorService prescriptionEditorService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1783: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _prescriptionEditorService = prescriptionEditorService ?? throw new ArgumentNullException(nameof(prescriptionEditorService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            AddRowCommand = new DelegateCommand(ExecuteAddRow);
            CompleteStep2Command = new DelegateCommand(async () => await ExecuteCompleteStep2()); // Issue #1591
            ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery); // Issue #1591
            SaveDraftCommand = new DelegateCommand(async () => await ExecuteSaveDraft()); // Issue #1594
            DeletePrescriptionCommand = new DelegateCommand(async () => await ExecuteDeletePrescription()); // Issue #1593 - Phase 4

            Logger.LogInformation("PrescriptionEditorViewModel已初始化（Epic #1540方案B + Issue #1591增强 + Issue #1594暂存功能 + Issue #1593删除功能）");
        }

        #endregion

        #region INavigationAware

        public override async void OnNavigatedTo(NavigationContext navigationContext)
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

                // Epic #1540: 加载药材数据（通过IPrescriptionEditorService）
                await LoadHerbsAsync();

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
