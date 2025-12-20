using System.Collections.ObjectModel;
using System.Text.Json;
using LYBT.Desktop.Foundation.Http; // OpenSpec: add-global-audit-system - IApiService
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 通用实体审计日志对话框视图模型
    /// OpenSpec: add-global-audit-system
    /// 支持查看任意实体类型的变更历史记录
    /// </summary>
    public class EntityAuditLogDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 私有字段

        private readonly IApiService _apiService;
        private string _title = "变更记录";
        private string _entityType = string.Empty;
        private Guid _entityId;
        private string _entityDescription = string.Empty;
        private ObservableCollection<AuditLogDisplayItem> _auditLogs = new();
        private AuditLogDisplayItem? _selectedLog;
        private bool _isLoading;
        private bool _showEmptyMessage;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount;
        private int _totalPages = 1;

        #endregion

        #region 公共属性

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public Guid EntityId
        {
            get => _entityId;
            set => SetProperty(ref _entityId, value);
        }

        public string EntityDescription
        {
            get => _entityDescription;
            set => SetProperty(ref _entityDescription, value);
        }

        public ObservableCollection<AuditLogDisplayItem> AuditLogs
        {
            get => _auditLogs;
            set => SetProperty(ref _auditLogs, value);
        }

        public AuditLogDisplayItem? SelectedLog
        {
            get => _selectedLog;
            set => SetProperty(ref _selectedLog, value);
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                UpdateEmptyMessage();
            }
        }

        public bool ShowEmptyMessage
        {
            get => _showEmptyMessage;
            set => SetProperty(ref _showEmptyMessage, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                SetProperty(ref _currentPage, value);
                RaisePropertyChanged(nameof(CanGoPrevious));
                RaisePropertyChanged(nameof(CanGoNext));
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                SetProperty(ref _totalPages, value);
                RaisePropertyChanged(nameof(CanGoPrevious));
                RaisePropertyChanged(nameof(CanGoNext));
            }
        }

        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        #endregion

        #region IDialogAware 实现

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("EntityType"))
                EntityType = parameters.GetValue<string>("EntityType");

            if (parameters.ContainsKey("EntityId"))
                EntityId = parameters.GetValue<Guid>("EntityId");

            if (parameters.ContainsKey("EntityDescription"))
                EntityDescription = parameters.GetValue<string>("EntityDescription");

            // 根据实体类型设置标题
            Title = GetTitleByEntityType(EntityType);

            Logger.LogInformation("EntityAuditLogDialog - 打开对话框，EntityType: {EntityType}, EntityId: {EntityId}",
                EntityType, EntityId);

            // 加载审计日志
            _ = LoadAuditLogsAsync();
        }

        public void OnDialogClosed()
        {
            Logger.LogInformation("EntityAuditLogDialog - 对话框已关闭");
        }

        #endregion

        #region 命令

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 构造函数

        public EntityAuditLogDialogViewModel(
            IApiService apiService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            RefreshCommand = new DelegateCommand(async () => await LoadAuditLogsAsync());
            CloseCommand = new DelegateCommand(ExecuteClose);
            PreviousPageCommand = new DelegateCommand(async () => await GoToPreviousPageAsync(), () => CanGoPrevious)
                .ObservesProperty(() => CanGoPrevious);
            NextPageCommand = new DelegateCommand(async () => await GoToNextPageAsync(), () => CanGoNext)
                .ObservesProperty(() => CanGoNext);
        }

        #endregion

        #region 私有方法

        private async Task LoadAuditLogsAsync()
        {
            if (string.IsNullOrEmpty(EntityType) || EntityId == Guid.Empty)
            {
                Logger.LogWarning("EntityAuditLogDialog - 缺少必要参数，EntityType: {EntityType}, EntityId: {EntityId}",
                    EntityType, EntityId);
                return;
            }

            IsLoading = true;
            AuditLogs.Clear();

            try
            {
                var endpoint = $"entityaudit/{EntityType}/{EntityId}?page={CurrentPage}&pageSize={_pageSize}";
                var result = await _apiService.GetAsync<PagedResult<EntityAuditLogDto>>(endpoint);

                if (result?.Items != null)
                {
                    TotalCount = result.TotalCount;
                    TotalPages = result.TotalPages;

                    foreach (var log in result.Items)
                    {
                        AuditLogs.Add(new AuditLogDisplayItem(log));
                    }

                    Logger.LogInformation("EntityAuditLogDialog - 加载成功，共 {Count} 条记录", TotalCount);
                }
                else
                {
                    Logger.LogWarning("EntityAuditLogDialog - 加载失败: {Message}", result?.ErrorMessage ?? "未知错误");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EntityAuditLogDialog - 加载审计日志异常");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToPreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadAuditLogsAsync();
            }
        }

        private async Task GoToNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadAuditLogsAsync();
            }
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        private void UpdateEmptyMessage()
        {
            ShowEmptyMessage = !IsLoading && AuditLogs.Count == 0;
        }

        private string GetTitleByEntityType(string entityType)
        {
            return entityType?.ToLower() switch
            {
                "patient" => "患者变更记录",
                "prescription" => "处方变更记录",
                "herb" => "药材变更记录",
                "formula" => "验方变更记录",
                "user" => "用户变更记录",
                "consultation" => "诊断变更记录",
                _ => "变更记录"
            };
        }

        #endregion
    }

    /// <summary>
    /// 审计日志显示项（用于UI绑定）
    /// </summary>
    public class AuditLogDisplayItem
    {
        private readonly EntityAuditLogDto _dto;

        public AuditLogDisplayItem(EntityAuditLogDto dto)
        {
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));
        }

        public Guid Id => _dto.Id;
        public string EntityType => _dto.EntityType;
        public Guid EntityId => _dto.EntityId;
        public Guid OperatorId => _dto.OperatorId;
        public string OperatorName => _dto.OperatorName;
        public string OperatorRoleDisplay => _dto.OperatorRoleDisplay;
        public string OperationTypeDisplay => _dto.OperationTypeDisplay;
        public string? Reason => _dto.Reason;
        public string CreatedAtDisplay => _dto.CreatedAtDisplay;

        /// <summary>
        /// 变更字段摘要（用于显示在列表中）
        /// </summary>
        public string ChangedFieldsSummary
        {
            get
            {
                if (string.IsNullOrEmpty(_dto.ChangedFields))
                    return "-";

                try
                {
                    var fields = JsonSerializer.Deserialize<List<string>>(_dto.ChangedFields);
                    if (fields == null || fields.Count == 0)
                        return "-";

                    // 翻译字段名称
                    var translatedFields = fields.Select(TranslateFieldName).ToList();

                    // 如果超过3个字段，显示前3个 + "等"
                    if (translatedFields.Count > 3)
                    {
                        return string.Join(", ", translatedFields.Take(3)) + $" 等{translatedFields.Count}项";
                    }

                    return string.Join(", ", translatedFields);
                }
                catch
                {
                    return _dto.ChangedFields ?? "-";
                }
            }
        }

        /// <summary>
        /// 翻译字段名称为中文
        /// </summary>
        private static string TranslateFieldName(string fieldName)
        {
            // 通用字段翻译
            return fieldName switch
            {
                "Name" => "名称",
                "Status" => "状态",
                "IsDeleted" => "删除状态",
                "Gender" => "性别",
                "BirthDate" => "出生日期",
                "PhoneNumber" => "手机号",
                "IdNumber" => "身份证号",
                "Address" => "地址",
                "AllergyHistory" => "过敏史",
                "MedicalHistory" => "既往病史",
                "PinYinCode" => "拼音码",
                "Email" => "邮箱",
                "RealName" => "真实姓名",
                "Role" => "角色",
                "ChineseName" => "中文名",
                "EnglishName" => "英文名",
                "Category" => "分类",
                "Properties" => "性味",
                "Meridians" => "归经",
                "Functions" => "功效",
                "Indications" => "主治",
                "Dosage" => "用量",
                "Contraindications" => "禁忌",
                "Notes" => "备注",
                // OpenSpec: refactor-diagnosis-fields - ChiefComplaint已从Consultation移除，保留映射用于查看历史审计记录
                "ChiefComplaint" => "主诉（已弃用）",
                "PresentIllness" => "现病史",
                "PastHistory" => "既往史",
                "Diagnosis" => "诊断",
                "Treatment" => "治疗",
                "Prescription" => "处方",
                "PrescriptionNumber" => "处方编号",
                "TotalPrice" => "总价",
                "Quantity" => "数量",
                "Unit" => "单位",
                "UnitPrice" => "单价",
                "PrintCount" => "打印次数",
                "Composition" => "组成",
                "Source" => "来源",
                "Description" => "描述",
                _ => fieldName
            };
        }
    }
}
