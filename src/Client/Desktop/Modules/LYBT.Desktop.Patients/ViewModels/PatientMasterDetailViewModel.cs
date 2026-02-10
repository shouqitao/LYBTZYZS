using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Desktop.Utilities.Excel;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者Master-Detail视图模型（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用IMasterDetailServices实现组合模式
    /// </summary>
    public partial class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>
    {
        private readonly PatientService _commandHandler;
        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _prismDialogService;
        private readonly ICommonDialogService? _commonDialogService;

        // OpenSpec: integrate-cardreader-module - 读卡器服务
        private readonly ICardReaderService _cardReaderService;
        private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;

        #region 扩展属性

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>性别选项</summary>
        public ObservableCollection<Gender> GenderOptions { get; } = new(Enum.GetValues<Gender>());

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; } = new(Enum.GetValues<CommonStatus>());

        /// <summary>详情标题</summary>
        public string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return "患者详情";
                if (IsNew) return "新增患者";
                return IsEditMode ? $"编辑患者 - {CurrentDetail.Name}" : $"患者详情 - {CurrentDetail.Name}";
            }
        }

        #endregion

        #region 读卡器属性 - OpenSpec: integrate-cardreader-module

        /// <summary>是否已连接读卡器</summary>
        public bool IsCardReaderConnected => _cardReaderService.IsConnected;

        /// <summary>是否正在读卡</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isReadingCard;
        public bool IsReadingCard
        {
            get => _isReadingCard;
            private set => SetProperty(ref _isReadingCard, value);
        }

        #endregion

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// </summary>
        // OpenSpec: integrate-cardreader-module - 添加读卡器服务依赖
        public PatientMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
            PatientService commandHandler,
            IPatientRepository patientRepository,
            IDialogService prismDialogService,
            ICardReaderService cardReaderService,
            IPatientCardReaderIntegration patientCardReaderIntegration,
            ICommonDialogService? commonDialogService = null)
            : base(viewModelServices, masterDetailServices)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _cardReaderService = cardReaderService ?? throw new ArgumentNullException(nameof(cardReaderService));
            _patientCardReaderIntegration = patientCardReaderIntegration ?? throw new ArgumentNullException(nameof(patientCardReaderIntegration));
            _commonDialogService = commonDialogService;

            PageTitle = "患者管理";

            // 监听属性变化
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(CurrentDetail) or nameof(IsEditMode))
                {
                    OnPropertyChanged(nameof(DetailTitle));
                }
            };
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
                    var pagedData = await _patientRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    MasterDetailServices.Pagination.TotalCount = pagedData.TotalCount;

                    Items.Clear();
                    foreach (var item in pagedData.Items ?? Enumerable.Empty<PatientListDto>())
                    {
                        Items.Add(item);
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
                var patient = await _patientRepository.GetByIdAsync(item.Id);
                if (patient == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync($"患者 '{item.Name}' 不存在或已被删除", "加载失败");
                    return;
                }

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

                MasterDetailServices.DetailEditor.LoadDetail(detail);
                OnPropertyChanged(nameof(DetailTitle));
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
            var detail = PatientDetailModel.CreateNew();
            OnPropertyChanged(nameof(DetailTitle));
            return detail;
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(PatientDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("患者姓名不能为空", "验证失败");
                return false;
            }

            try
            {
                var dto = new PatientInputDto
                {
                    Id = detail.Id,
                    Name = detail.Name.Trim(),
                    PinYinCode = detail.PinYinCode?.Trim(),
                    Gender = detail.Gender,
                    BirthDate = detail.BirthDate,
                    IdNumber = detail.IdNumber?.Trim(),
                    PhoneNumber = detail.PhoneNumber?.Trim(),
                    Address = detail.Address?.Trim()
                };

                var result = IsNew
                    ? await _patientRepository.CreateAsync(dto)
                    : await _patientRepository.UpdateAsync(dto);

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

                Logger.LogInformation("患者{Action}成功: {PatientId} - {PatientName}",
                    IsNew ? "创建" : "更新", result.Id, result.Name);

                OnPropertyChanged(nameof(DetailTitle));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存患者失败: {PatientName}", detail.Name);
                var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                    IsNew ? "创建患者" : "更新患者", ex);
                MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
                return false;
            }
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(PatientListDto item)
        {
            var result = await _commandHandler.DeletePatientAsync(item.Id);
            if (!result.Success)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", result.Error ?? $"删除患者 '{item.Name}' 失败");
            }
            else
            {
                Logger.LogInformation("患者删除成功: {PatientId} - {PatientName}", item.Id, item.Name);
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

            try
            {
                var patient = SelectedItem;
                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync($"确认恢复患者 [{patient.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _patientRepository.RestoreAsync(patient.Id);
                if (result != null)
                {
                    Logger.LogInformation("患者已恢复: {PatientName}", patient.Name);
                    await MasterDetailServices.Dialog.ShowSuccessAsync($"患者 '{patient.Name}' 已恢复", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("恢复患者失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复患者失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("恢复患者失败", "操作失败");
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>导入患者</summary>
        [RelayCommand]
        private async Task ImportAsync()
        {
            if (_commonDialogService == null) return;

            try
            {
                var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择患者导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                var patients = await ExcelHelper.ParseAsync<PatientInputDto>(fileStream, hasHeader: true);
                if (patients == null || patients.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("文件中没有有效的患者数据", "导入患者");
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
                    await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者");
                    return;
                }

                var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：";
                    foreach (var f in result.Failures.Take(3))
                        message += $"\n第{f.OriginalRowNumber}行：{f.FailureReason}";
                }
                await _commonDialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入患者失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("导入患者失败", "操作失败");
            }
        }

        /// <summary>导出患者</summary>
        [RelayCommand]
        private async Task ExportAsync()
        {
            if (_commonDialogService == null) return;

            try
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出患者数据",
                    defaultFileName: $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                var allPatients = await _patientRepository.SearchAsync(SearchText ?? string.Empty);
                if (allPatients == null || allPatients.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出患者");
                    return;
                }

                await ExcelHelper.ExportAsync(allPatients, filePath, "患者数据");
                await _commonDialogService.ShowInfoAsync($"成功导出{allPatients.Count}条患者数据到：\n{filePath}", "导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出患者失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("导出患者失败", "操作失败");
            }
        }

        /// <summary>下载模板</summary>
        [RelayCommand]
        private async Task DownloadTemplateAsync()
        {
            if (_commonDialogService == null) return;

            try
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存患者导入模板",
                    defaultFileName: $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                var sampleData = new List<PatientInputDto>
                {
                    new() { Name = "张三", Gender = Gender.Male, BirthDate = new DateTime(1980, 1, 1), PhoneNumber = "13800138000", Address = "北京市朝阳区" },
                    new() { Name = "李四", Gender = Gender.Female, BirthDate = new DateTime(1990, 5, 15), PhoneNumber = "13800138001", Address = "上海市浦东新区" }
                };
                await ExcelHelper.GenerateTemplateAsync(filePath, "患者导入模板", sampleData);
                await _commonDialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。", "下载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "下载模板失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("下载模板失败", "操作失败");
            }
        }

        /// <summary>查看病历</summary>
        [RelayCommand(CanExecute = nameof(CanViewMedicalRecords))]
        private void ViewMedicalRecords()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("查看患者病历：{PatientId}", SelectedItem.Id);
            // TODO: 导航到病历查看页面
        }

        private bool CanViewMedicalRecords() => HasSelection;

        /// <summary>新建问诊</summary>
        [RelayCommand(CanExecute = nameof(CanNewConsultation))]
        private void NewConsultation()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("为患者新建问诊：{PatientId}", SelectedItem.Id);
            // TODO: 导航到问诊流程页面
        }

        private bool CanNewConsultation() => HasSelection;

        #endregion

        #region 读卡器命令 - OpenSpec: integrate-cardreader-module

        /// <summary>刷卡录入命令</summary>
        [RelayCommand(CanExecute = nameof(CanReadCard))]
        private async Task ReadCardAsync()
        {
            if (!_cardReaderService.IsConnected)
            {
                // 尝试初始化读卡器
                var initialized = await _cardReaderService.InitializeAsync();
                if (!initialized)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("读卡器未连接，请检查设备", "读卡器未连接");
                    return;
                }
            }

            try
            {
                IsReadingCard = true;
                MasterDetailServices.Loading.BeginLoading("正在读取身份证...");

                var result = await _cardReaderService.ReadCardAsync();
                if (!result.IsSuccess)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync($"读卡失败：{result.ErrorMessage}", "读卡失败");
                    return;
                }

                Logger.LogInformation("读卡成功：{Name}，身份证号：{IdNumber}", result.Name, MaskIdNumber(result.IdNumber));

                // 查找患者
                var existingPatient = await _patientCardReaderIntegration.FindPatientByIdNumberAsync(result.IdNumber);
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "读卡时发生异常");
                await MasterDetailServices.Dialog.ShowErrorAsync("读卡失败，请重试", "读卡失败");
            }
            finally
            {
                IsReadingCard = false;
                MasterDetailServices.Loading.EndLoading();
            }
        }

        private bool CanReadCard() => !IsReadingCard;

        /// <summary>处理新患者（从读卡结果创建）</summary>
        private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)
        {
            var message = $"未找到患者记录：{cardResult.Name}\n" +
                         $"身份证号：{MaskIdNumber(cardResult.IdNumber)}\n\n" +
                         "是否创建新患者档案？";

            var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(message, "创建新患者");

            if (confirmed)
            {
                // 创建新患者并选中
                var patientResult = await _patientCardReaderIntegration.FindOrCreatePatientAsync(cardResult);
                Logger.LogInformation("患者创建成功：{PatientId}, {Name}", patientResult.PatientId, patientResult.Name);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"患者 {patientResult.Name} 创建成功", "创建成功");

                // 刷新列表并选中新患者
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

        /// <summary>掩码身份证号（保护隐私）</summary>
        private static string MaskIdNumber(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
                return idNumber;
            return idNumber[..6] + "****" + idNumber[^4..];
        }

        #endregion
    }
}
