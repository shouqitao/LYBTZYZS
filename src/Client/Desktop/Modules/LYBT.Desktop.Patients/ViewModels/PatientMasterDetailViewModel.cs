using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Patients.Mappers;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Infrastructure.CardReader.Models;
using Microsoft.Win32;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者Master-Detail视图模型（组合模式）
    ///
    /// 使用IPatientService单依赖 + PatientEditor子VM模式
    /// 所有编辑操作通过PatientEditor封装
    /// </summary>
    public partial class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>
    {
        private readonly IPatientService _patientService;
        private readonly IPatientStatusHandler _statusHandler;
        private readonly IDesktopCacheManager _cacheManager;
        private readonly PatientMapper _patientMapper;

        // Child ViewModels
        private readonly PatientCardReaderViewModel _cardReaderViewModel;

        /// <summary>患者编辑子 VM</summary>
        public PatientEditorViewModel PatientEditor { get; }

        #region 扩展属性

        /// <inheritdoc/>
        protected override string EntityDisplayName => "患者";

        /// <inheritdoc/>
        protected override string NewEntityVerb => "新增";

        /// <inheritdoc/>
        protected override string? GetDetailDisplayName() => CurrentDetail?.Name;

        /// <summary>性别选项</summary>
        public ObservableCollection<Gender> GenderOptions { get; } = new(Enum.GetValues<Gender>());

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; } = new(CommonOptions.StatusOptions);

        #endregion

        #region Child ViewModels

        /// <summary>读卡器功能 ViewModel</summary>
        public PatientCardReaderViewModel CardReaderViewModel => _cardReaderViewModel;

        #endregion

        #region 读卡器属性 - 代理到 Child ViewModel

        /// <summary>是否已连接读卡器</summary>
        public bool IsCardReaderConnected => _cardReaderViewModel.IsCardReaderConnected;

        /// <summary>是否正在读卡</summary>
        public bool IsReadingCard => _cardReaderViewModel.IsReadingCard;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public PatientMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
            IPatientService patientService,
            IPatientStatusHandler statusHandler,
            IDesktopCacheManager cacheManager,
            PatientMapper patientMapper,
            // Child ViewModels
            PatientCardReaderViewModel cardReaderViewModel,
            PatientEditorViewModel patientEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _patientMapper = patientMapper ?? throw new ArgumentNullException(nameof(patientMapper));

            // Child ViewModels
            _cardReaderViewModel = cardReaderViewModel ?? throw new ArgumentNullException(nameof(cardReaderViewModel));
            PatientEditor = patientEditor ?? throw new ArgumentNullException(nameof(patientEditor));

            PageTitle = "患者管理";
        }

        #region 基类抽象方法实现

        /// <summary>加载列表数据</summary>
        protected override async Task LoadListAsync()
        {
            Logger.LogInformation("患者搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
                CurrentPage, PageSize, SearchText);

            try
            {
                await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    var pagedResult = await _patientService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    if (pagedResult.Data != null)
                    {
                        MasterDetailServices.Pagination.TotalCount = pagedResult.Data.TotalCount;

                        Items.Clear();
                        foreach (var item in pagedResult.Data.Items ?? Enumerable.Empty<PatientListDto>())
                        {
                            Items.Add(item);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取患者列表时发生异常");
                MasterDetailServices.ErrorHandler.HandleException(ex, "获取患者列表");
            }
        }

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(PatientListDto item)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(item.Id);
                if (result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync($"患者 '{item.Name}' 不存在或已被删除", "加载失败");
                    return;
                }

                PatientEditor.InitializeFromDto(result.Data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者详情失败: {PatientId}", item.Id);
                MasterDetailServices.ErrorHandler.HandleException(ex, "加载患者详情");
            }
        }

        /// <summary>创建新详情实例</summary>
        protected override PatientDetailModel CreateNewDetail()
        {
            PatientEditor.InitializeForNewCase();
            return new PatientDetailModel { Id = Guid.Empty };
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(PatientDetailModel detail)
        {
            if (!PatientEditor.Validate())
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("请修正验证错误后重试", "验证失败");
                return false;
            }

            try
            {
                var inputDto = PatientEditor.GetPatientData();
                var isEditingExisting = detail.Id != Guid.Empty;

                var result = isEditingExisting
                    ? await _patientService.UpdateAsync(inputDto)
                    : await _patientService.CreateAsync(inputDto);

                if (!result.Success)
                {
                    MasterDetailServices.ErrorHandler.SetError("Save", result.Error ?? "保存患者失败");
                    return false;
                }

                // 同步返回列表数据（D2: 改用 Mapperly ApplyToDetailModel 回填）
                _patientMapper.ApplyToDetailModel(detail, result.Data!);

                Logger.LogInformation("患者{Action}成功: {PatientId} - {PatientName}",
                    isEditingExisting ? "更新" : "创建", result.Data!.Id, result.Data!.Name);

                _cacheManager.InvalidatePatientCaches();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存患者失败: {PatientName}", detail.Name);
                var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                    detail.Id == Guid.Empty ? "创建患者" : "更新患者", ex);
                MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
                return false;
            }
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(PatientListDto item)
        {
            var result = await _patientService.DeleteAsync(item.Id, CancellationToken.None);
            if (!result.Success)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", result.Error ?? $"删除患者 '{item.Name}' 失败");
            }
            else
            {
                Logger.LogInformation("患者删除成功: {PatientId} - {PatientName}", item.Id, item.Name);
                _cacheManager.InvalidatePatientCaches();
            }
            return result.Success;
        }

        #endregion

        #region 扩展命令

        /// <inheritdoc/>
        protected override async Task InvalidateCachesAsync()
        {
            _cacheManager.InvalidatePatientCaches();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task RestoreItemAsync(PatientListDto item)
        {
            await _statusHandler.RestoreAsync(item);
        }

        /// <summary>批量删除（单次 batch-delete 调用，替代逐条删除）</summary>
        protected override async Task DeleteBatchAsync(List<PatientListDto> items)
        {
            var result = await _patientService.BatchDeleteAsync(items.Select(p => p.Id).ToList());
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("患者批量删除完成: 成功 {Success}, 失败 {Failure}",
                    result.Data.SuccessCount, result.Data.FailureCount);
                _cacheManager.InvalidatePatientCaches();
            }
            else
            {
                MasterDetailServices.ErrorHandler.SetError("BatchDelete", result.Error ?? "批量删除患者失败");
            }
        }

        /// <summary>查看医案</summary>
        [RelayCommand(CanExecute = nameof(CanViewMedicalRecords))]
        private void ViewMedicalRecords()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("查看患者医案：{PatientId}", SelectedItem.Id);
            // FUTURE: 导航到医案查看页面 (US-MC-010)
        }

        private bool CanViewMedicalRecords() => HasSelection;

        /// <summary>新建医案</summary>
        [RelayCommand(CanExecute = nameof(CanNewConsultation))]
        private void NewConsultation()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("为患者新建医案：{PatientId}", SelectedItem.Id);
            // FUTURE: 导航到新建医案流程页面 (US-MC-003)
        }

        private bool CanNewConsultation() => HasSelection;

        #endregion

        #region 批量导入/导出命令

        /// <summary>导入 JSON 文件反序列化选项（camelCase + 枚举字符串，ADR-0022 对齐）</summary>
        private static readonly JsonSerializerOptions ImportJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>下载导入模板（US-PAT-011）</summary>
        [RelayCommand]
        private async Task DownloadImportTemplateAsync()
        {
            try
            {
                var result = await _patientService.ExportTemplateAsync();
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "下载导入模板失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = "患者导入模板.json",
                    Title = "保存导入模板"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"模板已保存到：{dialog.FileName}", "下载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "下载患者导入模板失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("下载模板", ex), "操作失败");
            }
        }

        /// <summary>批量导入患者（US-PAT-011/003）</summary>
        [RelayCommand]
        private async Task ImportPatientsAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 文件|*.json",
                Title = "选择患者导入文件"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var request = JsonSerializer.Deserialize<PatientBatchImportInputDto>(json, ImportJsonOptions);
                if (request?.Patients == null || request.Patients.Count == 0)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("文件中没有患者数据，请检查格式", "导入失败");
                    return;
                }

                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(
                    $"将导入 {request.Patients.Count} 条患者记录（重复策略：{request.Strategy}），是否继续？", "确认导入");
                if (!confirmed) return;

                var result = await _patientService.BatchImportAsync(request);
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "批量导入失败", "操作失败");
                    return;
                }

                var data = result.Data;
                var msg = $"导入完成：成功 {data.SuccessCount} 条，失败 {data.FailureCount} 条，跳过 {data.SkippedCount} 条";
                await MasterDetailServices.Dialog.ShowSuccessAsync(msg, "导入结果");
                _cacheManager.InvalidatePatientCaches();
                await RefreshAsync();
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "解析患者导入文件失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync("文件格式错误，请使用下载的 JSON 模板", "导入失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量导入患者失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入患者", ex), "操作失败");
            }
        }

        /// <summary>导出患者数据（US-PAT-012）</summary>
        [RelayCommand]
        private async Task ExportPatientsAsync()
        {
            try
            {
                var result = await _patientService.ExportPatientsAsync(SearchText);
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "导出患者数据失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = $"患者导出_{DateTime.Now:yyyyMMddHHmmss}.json",
                    Title = "保存导出文件"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"已导出 {result.Data.Length} 字节到：{dialog.FileName}", "导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出患者数据失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出患者", ex), "操作失败");
            }
        }

        #endregion

        #region 读卡器命令

        /// <summary>刷卡录入命令 - 委托给 Child ViewModel</summary>
        [RelayCommand(CanExecute = nameof(CanReadCard))]
        private async Task ReadCardAsync()
        {
            MasterDetailServices.Loading.BeginLoading("正在读取身份证...");

            try
            {
                var result = await _cardReaderViewModel.ReadCardAsync();
                if (result == null) return;

                Logger.LogInformation("读卡成功：{Name}，身份证号：{IdNumber}", result.Name, PatientCardReaderViewModel.MaskIdNumber(result.IdNumber));

                // 查找患者
                var existingPatient = await _cardReaderViewModel.FindPatientByIdNumberAsync(result.IdNumber);
                if (existingPatient != null)
                {
                    // 找到患者，选中并显示
                    await MasterDetailServices.Dialog.ShowSuccessAsync($"找到患者：{existingPatient.Name}", "查找成功");
                    await SearchAndSelectPatientAsync(existingPatient.PatientId);
                }
                else
                {
                    // 未找到患者，询问是否创建
                    await HandleNewPatientFromCardAsync(result);
                }
            }
            finally
            {
                MasterDetailServices.Loading.EndLoading();
            }
        }

        private bool CanReadCard() => !_cardReaderViewModel.IsReadingCard;

        /// <summary>处理新患者（从读卡结果创建）</summary>
        private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)
        {
            var message = $"未找到患者记录：{cardResult.Name}\n" +
                         $"身份证号：{PatientCardReaderViewModel.MaskIdNumber(cardResult.IdNumber)}\n\n" +
                         "是否创建新患者档案？";

            var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(message, "创建新患者");

            if (confirmed)
            {
                // 创建新患者并选中
                var patientResult = await _cardReaderViewModel.FindOrCreatePatientAsync(cardResult);
                Logger.LogInformation("患者创建成功：{PatientId}, {Name}", patientResult.PatientId, patientResult.Name);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"患者 {patientResult.Name} 创建成功", "创建成功");

                // 刷新列表并选中新患者
                _cacheManager.InvalidatePatientCaches();
                await RefreshAsync();
                await SearchAndSelectPatientAsync(patientResult.PatientId);
            }
        }

        /// <summary>搜索并选中患者</summary>
        private async Task SearchAndSelectPatientAsync(Guid patientId)
        {
            // 刷新列表
            await RefreshAsync();

            // 在列表中查找并选中
            var patient = Items.FirstOrDefault(p => p.Id == patientId);
            if (patient != null)
            {
                SelectedItem = patient;
            }
        }

        #endregion
    }
}
