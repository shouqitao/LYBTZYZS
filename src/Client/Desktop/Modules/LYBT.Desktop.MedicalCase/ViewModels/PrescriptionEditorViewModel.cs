using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services; // Issue #1790: 引入Manager服务
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

        // Issue #1790: 组件化服务 - 药材过滤和验证逻辑
        private readonly PrescriptionEditorHerbFilterManager _herbFilterManager;
        private readonly PrescriptionEditorValidator _validator;

        // Issue #1807: 组件化服务 Phase 2 - 价格计算、验方导入、药材选择
        private readonly PrescriptionCalculator _calculator;
        private readonly FormulaImportHandler _formulaImportHandler;
        private readonly HerbSelectionManager _herbSelectionManager;

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
        /// Issue #1807: 委托给HerbSelectionManager
        /// </summary>
        public ObservableCollection<SimpleItemRow> ItemRows => _herbSelectionManager.ItemRows;

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
        /// Issue #1807: 委托给PrescriptionCalculator
        /// </summary>
        public decimal SingleDosagePrice
        {
            get
            {
                var allItems = GetAllItems();
                return _calculator?.CalculateSingleDosagePrice(allItems, AllHerbs) ?? 0m;
            }
        }

        /// <summary>
        /// 总价格（单剂价格 × 剂数）
        /// Issue #1807: 委托给PrescriptionCalculator
        /// </summary>
        public decimal TotalPrice => _calculator?.CalculateTotalPrice(SingleDosagePrice, DosageCount) ?? 0m;

        /// <summary>
        /// 药材总数
        /// Issue #1807: 委托给HerbSelectionManager
        /// </summary>
        public int ItemCount => _herbSelectionManager?.ItemCount ?? 0;

        /// <summary>
        /// 所有药材数据（Issue #1790: 委托给HerbFilterManager）
        /// </summary>
        private List<HerbDto> AllHerbs => _herbFilterManager.AllHerbs;

        /// <summary>
        /// 过滤后的药材列表（ComboBox绑定）
        /// Issue #1790: 委托给HerbFilterManager
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs => _herbFilterManager.FilteredHerbs;

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
        /// Issue #1790: 委托给PrescriptionEditorValidator
        /// </summary>
        public bool Validate()
        {
            var allItems = GetAllItems();
            var result = _validator.Validate(CurrentPatient, MedicalCaseId, allItems, AllHerbs);

            ValidationMessage = result.ValidationMessage;
            return result.IsValid;
        }

        // Issue #1790: ValidateBasicInfo, ValidateHerbItems, ValidateSingleHerbItem, HandleValidationResult
        // 已移至PrescriptionEditorValidator

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存处方 - Epic #1540: 使用IPrescriptionEditorService构建草稿
        /// Issue #1477协调：此方法构建草稿，最终写入由MedicalCase聚合根控制
        /// Issue #1794: 优化方法长度（86→35行）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存处方...");

                if (!Validate())
                {
                    Logger.LogWarning("处方验证失败：{Message}", ValidationMessage);
                    return false;
                }

                var createDto = BuildPrescriptionCreateDto();
                var draft = await _prescriptionEditorService.BuildPrescriptionDraftAsync(createDto);

                if (!await ValidateDraftAsync(draft))
                    return false;

                var totalAmount = await CalculateAndLogTotalAmountAsync(draft);

                var savedPrescription = await SaveAndHandleResultAsync(createDto, draft.Id, totalAmount, draft.Items.Count);
                if (savedPrescription == null)
                    return false;

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

        /// <summary>
        /// 构建处方创建DTO
        /// Issue #1794: 从SaveAsync提取，封装DTO构建逻辑
        /// </summary>
        private PrescriptionCreateDto BuildPrescriptionCreateDto()
        {
            var allItems = GetAllItems();

            // Issue #1807: 委托给 PrescriptionCalculator 计算价格
            var itemsWithPrice = _calculator.BuildItemsWithPrice(allItems, AllHerbs);

            return new PrescriptionCreateDto
            {
                PatientId = CurrentPatient!.Id,
                DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                ConsultationId = MedicalCaseId,
                Quantity = DosageCount,
                Usage = Usage,
                Advice = MedicalAdvice,
                Notes = Remark,
                Items = itemsWithPrice
            };
        }

        /// <summary>
        /// 验证处方草稿（调用Service层验证）
        /// Issue #1790: 委托给PrescriptionEditorValidator
        /// </summary>
        private async Task<bool> ValidateDraftAsync(PrescriptionDto draft)
        {
            var isValid = await _validator.ValidateDraftAsync(draft);
            if (!isValid)
            {
                await ShowErrorMessageAsync("处方数据验证失败，请检查药材信息");
            }
            return isValid;
        }

        /// <summary>
        /// 计算总金额并记录日志
        /// Issue #1794: 从SaveAsync提取，封装金额计算和日志记录
        /// Issue #1807: 委托给 PrescriptionCalculator 计算
        /// </summary>
        private async Task<decimal> CalculateAndLogTotalAmountAsync(PrescriptionDto draft)
        {
            // Issue #1807: 委托给 calculator 组件计算总金额
            var totalAmount = _calculator.CalculateAndLogTotalAmount(draft.Items, DosageCount);
            return await Task.FromResult(totalAmount);
        }

        /// <summary>
        /// 保存处方并处理结果
        /// Issue #1794: 从SaveAsync提取，封装保存和结果处理逻辑
        /// </summary>
        private async Task<PrescriptionDto?> SaveAndHandleResultAsync(PrescriptionCreateDto createDto, Guid draftId, decimal totalAmount, int itemCount)
        {
            var savedPrescription = await SavePrescriptionAndUpdateMedicalCaseAsync(createDto, draftId);
            if (savedPrescription != null)
            {
                await ShowSuccessMessageAsync($"处方已保存（{itemCount}味药材，总价{totalAmount:F2}元）");
            }
            else
            {
                await ShowErrorMessageAsync("处方保存失败");
            }
            return savedPrescription;
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
        /// 加载所有药材数据
        /// Issue #1790: 委托给PrescriptionEditorHerbFilterManager
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载药材数据...");
                await _herbFilterManager.LoadHerbsAsync();
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
        /// Issue #1790: 委托给PrescriptionEditorHerbFilterManager
        /// </summary>
        public void FilterHerbs(string searchText)
        {
            try
            {
                _herbFilterManager.FilterHerbs(searchText);
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
        /// Issue #1807: 委托给HerbSelectionManager
        /// </summary>
        private void ExecuteAddRow()
        {
            _herbSelectionManager.AddRow();

            // 由于ItemsChanged事件会触发,不需要手动RaisePropertyChanged
        }

        /// <summary>
        /// 从ItemRows提取所有非空药材
        /// Issue #1807: 委托给HerbSelectionManager
        /// </summary>
        private List<PrescriptionItemDto> GetAllItems()
        {
            return _herbSelectionManager.GetAllValidItems();
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
        /// 显示验方库对话框
        /// Issue #1807: 委托给FormulaImportHandler
        /// </summary>
        private async void ExecuteShowOtherCasesQuery()
        {
            try
            {
                Logger.LogInformation("显示验方库对话框");
                var (success, errorMessage) = await _formulaImportHandler.ShowFormulaLibraryAsync();

                if (!success)
                {
                    await ShowWarningMessageAsync(errorMessage ?? "验方库功能暂未实现");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示验方库对话框失败");
                await ShowErrorMessageAsync($"显示验方库失败：{ex.Message}");
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
                // Issue #1807: 委托给 HerbSelectionManager 清空和初始化
                _herbSelectionManager.ClearAll();
                DosageCount = 7;
                Usage = "水煎服，日一剂，早晚分服";
                MedicalAdvice = string.Empty;
                Remark = string.Empty;
                TreatmentMethod = string.Empty;
                TreatmentPrinciple = string.Empty;
                Step2CompletedAt = null;
                DuplicateHerbsWarningText = string.Empty;

                // 重新添加初始行
                // Issue #1807: 委托给 HerbSelectionManager 初始化5行
                _herbSelectionManager.InitializeRows(5);

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
        /// Issue #1790: 委托给PrescriptionEditorValidator
        /// </summary>
        private void CheckDuplicateHerbs()
        {
            var allItems = GetAllItems();
            DuplicateHerbsWarningText = _validator.CheckDuplicateHerbs(allItems);
        }

        /// <summary>
        /// 药材加载完成事件处理
        /// Issue #1790: 响应HerbFilterManager事件
        /// </summary>
        private void OnHerbsLoaded(object? sender, HerbsLoadedEventArgs e)
        {
            Logger.LogInformation("药材加载完成事件触发，共{Count}味", e.HerbCount);
        }

        /// <summary>
        /// 验证完成事件处理
        /// Issue #1790: 响应Validator事件
        /// </summary>
        private void OnValidationCompleted(object? sender, ValidationResultEventArgs e)
        {
            if (!e.Result.IsValid)
            {
                Logger.LogWarning("验证完成事件触发，验证失败：{Message}", e.Result.ValidationMessage);
            }
            else
            {
                Logger.LogInformation("验证完成事件触发，验证成功，共{ItemCount}味药材", e.Result.ItemCount);
            }
        }

        #endregion

        #region 构造函数

        public PrescriptionEditorViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IPrescriptionEditorService prescriptionEditorService,
            IDialogService dialogService,
            PrescriptionEditorHerbFilterManager herbFilterManager, // Issue #1790
            PrescriptionEditorValidator validator, // Issue #1790
            PrescriptionCalculator calculator, // Issue #1807
            FormulaImportHandler formulaImportHandler, // Issue #1807
            HerbSelectionManager herbSelectionManager, // Issue #1807
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

            // Issue #1790: 注入Manager服务
            _herbFilterManager = herbFilterManager ?? throw new ArgumentNullException(nameof(herbFilterManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            // Issue #1807: 注入组件化服务 Phase 2
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _formulaImportHandler = formulaImportHandler ?? throw new ArgumentNullException(nameof(formulaImportHandler));
            _herbSelectionManager = herbSelectionManager ?? throw new ArgumentNullException(nameof(herbSelectionManager));

            // Issue #1790: 订阅Manager事件
            _herbFilterManager.HerbsLoaded += OnHerbsLoaded;
            _validator.ValidationCompleted += OnValidationCompleted;

            // Issue #1807: 订阅组件事件
            _calculator.PriceCalculated += OnPriceCalculated;
            _formulaImportHandler.FormulaImported += OnFormulaImported;
            _herbSelectionManager.ItemsChanged += OnItemsChanged;

            // 初始化命令
            AddRowCommand = new DelegateCommand(ExecuteAddRow);
            CompleteStep2Command = new DelegateCommand(async () => await ExecuteCompleteStep2()); // Issue #1591
            ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery); // Issue #1591
            SaveDraftCommand = new DelegateCommand(async () => await ExecuteSaveDraft()); // Issue #1594
            DeletePrescriptionCommand = new DelegateCommand(async () => await ExecuteDeletePrescription()); // Issue #1593 - Phase 4

            Logger.LogInformation("PrescriptionEditorViewModel已初始化（Epic #1540方案B + Issue #1591增强 + Issue #1594暂存功能 + Issue #1593删除功能 + Issue #1790组件化）");
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
                // Issue #1807: 委托给 HerbSelectionManager 初始化
                if (ItemRows.Count == 0)
                {
                    _herbSelectionManager.InitializeRows(5);
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

        #region Issue #1807: 组件事件处理器

        /// <summary>
        /// 处理价格计算完成事件
        /// </summary>
        private void OnPriceCalculated(object? sender, PriceCalculatedEventArgs e)
        {
            Logger.LogInformation("价格计算完成事件：单剂{SinglePrice:F2}元 × {DosageCount}剂 = 总价{TotalPrice:F2}元",
                e.SingleDosagePrice, e.DosageCount, e.TotalPrice);

            // 通知UI更新价格显示
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        /// <summary>
        /// 处理验方导入完成事件
        /// </summary>
        private void OnFormulaImported(object? sender, FormulaImportedEventArgs e)
        {
            if (e.Success && e.ImportedItems != null && e.ImportedItems.Count > 0)
            {
                Logger.LogInformation("验方导入成功：{ItemCount}味药材", e.ItemCount);

                // 应用导入的验方数据
                _herbSelectionManager.SetItems(e.ImportedItems);

                // 通知UI更新
                RaisePropertyChanged(nameof(ItemRows));
                RaisePropertyChanged(nameof(ItemCount));
                RaisePropertyChanged(nameof(SingleDosagePrice));
                RaisePropertyChanged(nameof(TotalPrice));
            }
            else
            {
                Logger.LogWarning("验方导入失败或取消：{ErrorMessage}", e.ErrorMessage);
            }
        }

        /// <summary>
        /// 处理药材列表变更事件
        /// </summary>
        private void OnItemsChanged(object? sender, ItemsChangedEventArgs e)
        {
            Logger.LogInformation("药材列表变更：{ChangeType}，当前{ItemCount}味药材",
                e.ChangeType, e.ItemCount);

            // 通知UI更新相关属性
            RaisePropertyChanged(nameof(ItemCount));
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// 释放资源，取消事件订阅
        /// Issue #1807: 取消组件事件订阅，防止内存泄漏
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消组件事件订阅
                if (_calculator != null)
                {
                    _calculator.PriceCalculated -= OnPriceCalculated;
                }

                if (_formulaImportHandler != null)
                {
                    _formulaImportHandler.FormulaImported -= OnFormulaImported;
                }

                if (_herbSelectionManager != null)
                {
                    _herbSelectionManager.ItemsChanged -= OnItemsChanged;
                }

                Logger.LogInformation("PrescriptionEditorViewModel 已释放资源");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
