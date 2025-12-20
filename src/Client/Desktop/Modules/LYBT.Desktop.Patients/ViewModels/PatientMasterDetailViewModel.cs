using System.Collections.ObjectModel;
using System.IO;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Events;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者Master-Detail视图模型
    /// OpenSpec: refactor-master-detail-layout
    /// OpenSpec: optimize-entity-data-flow - 使用PatientListDto优化列表加载
    ///
    /// 合并PatientManagementViewModel和PatientDetailViewModel功能
    /// </summary>
    public class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>
    {
        private readonly PatientCommandHandler _commandHandler;
        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _prismDialogService;

        #region 编辑属性

        private string _editName = string.Empty;
        private string _editPinYinCode = string.Empty;
        private Gender _editGender = Gender.Unknown;
        private DateTime? _editBirthDate;
        private string? _editIdNumber;
        private string? _editPhoneNumber;
        private string? _editAddress;
        private CommonStatus _editStatus = CommonStatus.Enabled;

        /// <summary>编辑-姓名</summary>
        public string EditName
        {
            get => _editName;
            set
            {
                if (SetProperty(ref _editName, value))
                {
                    EditPinYinCode = PinYinHelper.GetPinYinCode(value);
                    MarkAsModified();
                }
            }
        }

        /// <summary>编辑-拼音码（自动生成，可手动修正多音字错误）</summary>
        public string EditPinYinCode
        {
            get => _editPinYinCode;
            set { if (SetProperty(ref _editPinYinCode, value)) MarkAsModified(); }
        }

        /// <summary>编辑-性别</summary>
        public Gender EditGender
        {
            get => _editGender;
            set { if (SetProperty(ref _editGender, value)) MarkAsModified(); }
        }

        /// <summary>编辑-出生日期</summary>
        public DateTime? EditBirthDate
        {
            get => _editBirthDate;
            set
            {
                if (SetProperty(ref _editBirthDate, value))
                {
                    RaisePropertyChanged(nameof(EditAge));
                    MarkAsModified();
                }
            }
        }

        /// <summary>编辑-年龄</summary>
        public int? EditAge
        {
            get
            {
                if (!EditBirthDate.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - EditBirthDate.Value.Year;
                if (EditBirthDate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        /// <summary>编辑-身份证号</summary>
        public string? EditIdNumber
        {
            get => _editIdNumber;
            set { if (SetProperty(ref _editIdNumber, value)) MarkAsModified(); }
        }

        /// <summary>编辑-手机号</summary>
        public string? EditPhoneNumber
        {
            get => _editPhoneNumber;
            set { if (SetProperty(ref _editPhoneNumber, value)) MarkAsModified(); }
        }

        /// <summary>编辑-地址</summary>
        public string? EditAddress
        {
            get => _editAddress;
            set { if (SetProperty(ref _editAddress, value)) MarkAsModified(); }
        }

        /// <summary>编辑-状态</summary>
        public CommonStatus EditStatus
        {
            get => _editStatus;
            set { if (SetProperty(ref _editStatus, value)) MarkAsModified(); }
        }

        #endregion

        #region 选项列表

        /// <summary>性别选项</summary>
        public ObservableCollection<Gender> GenderOptions { get; }

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; }

        #endregion

        #region 扩展命令

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>审计日志命令</summary>
        public DelegateCommand<PatientListDto> ShowAuditLogCommand { get; private set; } = null!;

        /// <summary>恢复软删除数据命令</summary>
        public DelegateCommand<PatientListDto> RestoreCommand { get; private set; } = null!;

        /// <summary>导入命令</summary>
        public DelegateCommand ImportCommand { get; private set; } = null!;

        /// <summary>导出命令</summary>
        public DelegateCommand ExportCommand { get; private set; } = null!;

        /// <summary>下载模板命令</summary>
        public DelegateCommand DownloadTemplateCommand { get; private set; } = null!;

        #endregion

        #region 显示属性

        /// <summary>详情标题</summary>
        public string DetailTitle => CurrentDetail == null ? string.Empty :
            CurrentDetail.IsNew ? "新增患者" :
            IsEditMode ? $"编辑患者 - {CurrentDetail.Name}" :
            $"患者详情 - {CurrentDetail.Name}";

        #endregion

        #region 构造函数

        public PatientMasterDetailViewModel(
            PatientCommandHandler commandHandler,
            IPatientRepository patientRepository,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));

            PageTitle = "患者管理";
            PageSize = SystemConstants.DefaultPageSize;

            GenderOptions = new ObservableCollection<Gender>(Enum.GetValues<Gender>());
            StatusOptions = new ObservableCollection<CommonStatus>(Enum.GetValues<CommonStatus>());

            // 初始化扩展命令
            ShowAuditLogCommand = new DelegateCommand<PatientListDto>(ExecuteShowAuditLog, p => p != null);
            RestoreCommand = new DelegateCommand<PatientListDto>(async p => await RestoreAsync(p), p => p != null && !IsBusy && IsAdmin);
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            // 订阅事件
            EventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(async _ => await RefreshAsync());
            EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(async _ => await RefreshAsync());
        }

        #endregion

        #region 列表数据加载

        protected override async Task<IEnumerable<PatientListDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            // OpenSpec: optimize-entity-data-flow - 使用轻量级ListDto
            try
            {
                var result = await _patientRepository.GetPagedListAsync(page, pageSize, searchText);
                TotalCount = result.TotalCount;
                return result.Items ?? Enumerable.Empty<PatientListDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取患者列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"获取患者列表 - 模块:{nameof(PatientMasterDetailViewModel)}");
                TotalCount = 0;
                return Enumerable.Empty<PatientListDto>();
            }
        }

        #endregion

        #region Master-Detail抽象方法实现

        protected override async Task<PatientDetailModel?> LoadDetailAsync(PatientListDto item)
        {
            if (item == null) return null;

            var patient = await _patientRepository.GetByIdAsync(item.Id);
            if (patient == null) return null;

            var detail = new PatientDetailModel
            {
                Id = patient.Id,
                Name = patient.Name,
                PinYinCode = patient.PinYinCode ?? PinYinHelper.GetPinYinCode(patient.Name),
                Gender = patient.Gender,
                BirthDate = patient.BirthDate,
                IdNumber = patient.IdNumber,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                Status = patient.Status,
                VisitCount = patient.VisitCount
            };

            // 更新详情标题
            RaisePropertyChanged(nameof(DetailTitle));

            return detail;
        }

        protected override async Task<bool> SaveDetailAsync(PatientDetailModel detail)
        {
            if (detail == null) return false;

            // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端默认为Enabled
            var dto = new PatientInputDto
            {
                Id = detail.Id,
                Name = EditName.Trim(),
                PinYinCode = EditPinYinCode?.Trim(),
                Gender = EditGender,
                BirthDate = EditBirthDate,
                IdNumber = EditIdNumber?.Trim(),
                PhoneNumber = EditPhoneNumber?.Trim(),
                Address = EditAddress?.Trim()
            };

            var result = detail.IsNew
                ? await _patientRepository.CreateAsync(dto)
                : await _patientRepository.UpdateAsync(dto);

            if (result != null)
            {
                // 更新详情数据
                detail.Id = result.Id;
                detail.Name = result.Name;
                detail.PinYinCode = result.PinYinCode ?? PinYinHelper.GetPinYinCode(result.Name);
                detail.Gender = result.Gender;
                detail.BirthDate = result.BirthDate;
                detail.IdNumber = result.IdNumber;
                detail.PhoneNumber = result.PhoneNumber;
                detail.Address = result.Address;
                detail.Status = result.Status;

                // 发布事件
                if (dto.Id == Guid.Empty)
                    EventAggregator.GetEvent<PatientCreatedEvent>().Publish(result);
                else
                    EventAggregator.GetEvent<PatientUpdatedEvent>().Publish(result);

                RaisePropertyChanged(nameof(DetailTitle));
                return true;
            }

            return false;
        }

        protected override async Task<bool> DeleteDetailAsync(PatientDetailModel detail)
        {
            if (detail == null || detail.IsNew) return false;

            var result = await _commandHandler.DeletePatientAsync(detail.Id);
            return result.IsSuccess;
        }

        protected override PatientDetailModel CreateNewDetail()
        {
            var detail = PatientDetailModel.CreateNew();

            // 初始化编辑属性
            ClearEditProperties();

            RaisePropertyChanged(nameof(DetailTitle));
            return detail;
        }

        protected override PatientDetailModel CloneDetail(PatientDetailModel detail)
        {
            // 保存到编辑属性
            EditName = detail.Name;
            EditPinYinCode = detail.PinYinCode;
            EditGender = detail.Gender;
            EditBirthDate = detail.BirthDate;
            EditIdNumber = detail.IdNumber;
            EditPhoneNumber = detail.PhoneNumber;
            EditAddress = detail.Address;
            EditStatus = detail.Status;

            return detail.Clone();
        }

        protected override object? GetDetailId(PatientDetailModel detail)
        {
            return detail?.Id;
        }

        #endregion

        #region 删除操作

        protected override async Task OnExecuteDeleteAsync(PatientListDto item)
        {
            if (item == null) return;

            try
            {
                if (!await ShowConfirmationAsync($"确认删除患者 [{item.Name}] 吗？", "删除确认")) return;

                var result = await _commandHandler.DeletePatientAsync(item.Id);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync($"患者 [{item.Name}] 已删除");
                    await RefreshAsync();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "删除患者失败";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除患者时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, "删除患者");
            }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<PatientListDto> items)
        {
            if (items == null || items.Count == 0) return;

            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();

            foreach (var item in items)
            {
                try
                {
                    var result = await _commandHandler.DeletePatientAsync(item.Id);
                    if (result.IsSuccess) successCount++;
                    else { failureCount++; failedItems.Add($"{item.Name}（{result.ErrorMessage}）"); }
                }
                catch { failureCount++; failedItems.Add(item.Name); }
            }

            var message = $"批量删除完成！\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的患者：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5) message += $"等{failedItems.Count}个";
            }

            if (failureCount > 0) await ShowWarningMessageAsync(message);
            else await ShowSuccessMessageAsync(message);

            if (successCount > 0) await RefreshAsync();
        }

        #endregion

        #region 辅助方法

        private void ClearEditProperties()
        {
            EditName = string.Empty;
            EditPinYinCode = string.Empty;
            EditGender = Gender.Unknown;
            EditBirthDate = null;
            EditIdNumber = null;
            EditPhoneNumber = null;
            EditAddress = null;
            EditStatus = CommonStatus.Enabled;
        }

        #endregion

        #region 扩展命令实现

        private void ExecuteShowAuditLog(PatientListDto? patient)
        {
            if (patient == null) return;
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters
            {
                { "EntityType", "patient" },
                { "EntityId", patient.Id },
                { "EntityDescription", $"患者：{patient.Name}" }
            }, _ => { });
        }

        private async Task RestoreAsync(PatientListDto? patient)
        {
            if (patient == null) return;
            try
            {
                Logger.LogInformation("恢复软删除患者: {PatientId} - {PatientName}", patient.Id, patient.Name);
                var confirmed = await ShowConfirmationAsync($"确认恢复患者 [{patient.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _patientRepository.RestoreAsync(patient.Id);
                if (result != null)
                {
                    Logger.LogInformation("患者已恢复: {PatientName}", patient.Name);
                    await ShowSuccessMessageAsync($"患者 '{patient.Name}' 已恢复");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复患者失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复患者失败: {PatientId}", patient.Id);
                await ShowErrorMessageAsync("恢复患者失败");
            }
        }

        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await CommonDialogService!.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择患者导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                var patients = await Infrastructure.Helpers.ExcelHelper.ParseAsync<PatientInputDto>(fileStream, hasHeader: true);
                if (patients == null || patients.Count == 0)
                {
                    await CommonDialogService.ShowErrorAsync("文件中没有有效的患者数据", "导入患者");
                    return;
                }

                var request = new PatientBatchImportInputDto
                {
                    Patients = patients,
                    Strategy = DuplicateStrategy.Skip
                };
                var result = await _patientRepository.BatchImportAsync(request);
                if (result == null)
                {
                    await CommonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者");
                    return;
                }

                var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：";
                    foreach (var f in result.Failures.Take(3))
                        message += $"\n第{f.OriginalRowNumber}行：{f.FailureReason}";
                }
                await CommonDialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入患者");
        }

        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await CommonDialogService!.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出患者数据",
                    defaultFileName: $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                var allPatients = await _patientRepository.SearchAsync(SearchText ?? string.Empty);
                if (allPatients == null || allPatients.Count == 0)
                {
                    await CommonDialogService.ShowErrorAsync("没有可导出的数据", "导出患者");
                    return;
                }

                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allPatients, filePath, "患者数据");
                await CommonDialogService.ShowInfoAsync($"成功导出{allPatients.Count}条患者数据到：\n{filePath}", "导出成功");
            }, "导出患者");
        }

        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await CommonDialogService!.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存患者导入模板",
                    defaultFileName: $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除
                var sampleData = new List<PatientInputDto>
                {
                    new() { Name = "张三", Gender = Gender.Male, BirthDate = new DateTime(1980, 1, 1), PhoneNumber = "13800138000", Address = "北京市朝阳区" },
                    new() { Name = "李四", Gender = Gender.Female, BirthDate = new DateTime(1990, 5, 15), PhoneNumber = "13800138001", Address = "上海市浦东新区" }
                };
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "患者导入模板", sampleData);
                await CommonDialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。", "下载成功");
            }, "下载模板");
        }

        #endregion

        #region 命令状态刷新

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            ShowAuditLogCommand?.RaiseCanExecuteChanged();
            RestoreCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
