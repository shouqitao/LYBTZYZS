using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.CardReader.Models;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者Master-Detail视图模型（组合模式）
    /// OpenSpec: frontend-architecture-unification
    ///
    /// 使用IPatientService单依赖 + PatientEditor子VM模式
    /// 所有编辑操作通过PatientEditor封装
    /// </summary>
    public partial class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>
    {
        private readonly IPatientService _patientService;
        private readonly IPatientStatusHandler _statusHandler;
        private readonly IDesktopCacheManager _cacheManager;

        // Child ViewModels
        private readonly PatientCardReaderViewModel _cardReaderViewModel;
        private readonly PatientImportExportViewModel _importExportViewModel;

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

        /// <summary>导入导出功能 ViewModel</summary>
        public PatientImportExportViewModel ImportExportViewModel => _importExportViewModel;

        #endregion

        #region 读卡器属性 - 代理到 Child ViewModel

        /// <summary>是否已连接读卡器</summary>
        public bool IsCardReaderConnected => _cardReaderViewModel.IsCardReaderConnected;

        /// <summary>是否正在读卡</summary>
        public bool IsReadingCard => _cardReaderViewModel.IsReadingCard;

        #endregion

        /// <summary>
        /// 构造函数
        /// OpenSpec: frontend-architecture-unification - Service单依赖 + 子VM组合
        /// </summary>
        public PatientMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
            IPatientService patientService,
            IPatientStatusHandler statusHandler,
            IDesktopCacheManager cacheManager,
            // Child ViewModels
            PatientCardReaderViewModel cardReaderViewModel,
            PatientImportExportViewModel importExportViewModel,
            PatientEditorViewModel patientEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

            // Child ViewModels
            _cardReaderViewModel = cardReaderViewModel ?? throw new ArgumentNullException(nameof(cardReaderViewModel));
            _importExportViewModel = importExportViewModel ?? throw new ArgumentNullException(nameof(importExportViewModel));
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
                    var pagedResult = await _patientService.GetPatientsPagedAsync(CurrentPage, PageSize, SearchText);
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
                    ? await _patientService.UpdatePatientAsync(inputDto)
                    : await _patientService.CreatePatientAsync(inputDto);

                if (!result.Success)
                {
                    MasterDetailServices.ErrorHandler.SetError("Save", result.Error ?? "保存患者失败");
                    return false;
                }

                // 同步返回列表数据
                detail.Id = result.Data!.Id;
                detail.Name = result.Data.Name;
                detail.PinYinCode = result.Data.PinYinCode ?? string.Empty;
                detail.Gender = result.Data.Gender;
                detail.BirthDate = result.Data.BirthDate;
                detail.IdNumber = result.Data.IdNumber;
                detail.PhoneNumber = result.Data.PhoneNumber;
                detail.Address = result.Data.Address;
                detail.Status = result.Data.Status;

                Logger.LogInformation("患者{Action}成功: {PatientId} - {PatientName}",
                    isEditingExisting ? "更新" : "创建", result.Data.Id, result.Data.Name);

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
            var result = await _patientService.DeletePatientAsync(item.Id, CancellationToken.None);
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

        /// <summary>恢复软删除</summary>
        [RelayCommand(CanExecute = nameof(CanRestore))]
        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;
            if (await _statusHandler.RestoreAsync(SelectedItem))
            {
                _cacheManager.InvalidatePatientCaches();
                await RefreshAsync();
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>导入患者</summary>
        [RelayCommand]
        private async Task ImportAsync()
        {
            if (await _importExportViewModel.ImportAsync())
            {
                _cacheManager.InvalidatePatientCaches();
                await RefreshAsync();
            }
        }

        /// <summary>导出患者</summary>
        [RelayCommand]
        private async Task ExportAsync()
        {
            await _importExportViewModel.ExportAsync(SearchText);
        }

        /// <summary>下载模板</summary>
        [RelayCommand]
        private async Task DownloadTemplateAsync()
        {
            await _importExportViewModel.DownloadTemplateAsync();
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
