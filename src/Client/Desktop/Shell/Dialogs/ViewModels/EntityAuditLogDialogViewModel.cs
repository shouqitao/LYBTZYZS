using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 通用实体审计日志对话框视图模型
    /// OpenSpec: add-global-audit-system
    /// OpenSpec: standardize-viewmodel-framework - 迁移到DialogViewModelBase
    /// 支持查看任意实体类型的变更历史记录
    /// </summary>
    public partial class EntityAuditLogDialogViewModel : DialogViewModelBase
    {
        #region 私有字段

        private readonly IApiService _apiService;
        private readonly int _pageSize = 20;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 实体类型
        /// </summary>
        [ObservableProperty]
        private string _entityType = string.Empty;

        /// <summary>
        /// 实体ID
        /// </summary>
        [ObservableProperty]
        private Guid _entityId;

        /// <summary>
        /// 实体描述
        /// </summary>
        [ObservableProperty]
        private string _entityDescription = string.Empty;

        /// <summary>
        /// 审计日志列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AuditLogDisplayItem> _auditLogs = new();

        /// <summary>
        /// 选中的日志项
        /// </summary>
        [ObservableProperty]
        private AuditLogDisplayItem? _selectedLog;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyMessage))]
        private bool _isLoading;

        /// <summary>
        /// 当前页码
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        private int _currentPage = 1;

        /// <summary>
        /// 总记录数
        /// </summary>
        [ObservableProperty]
        private int _totalCount;

        /// <summary>
        /// 总页数
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        private int _totalPages = 1;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否显示空消息
        /// </summary>
        public bool ShowEmptyMessage => !IsLoading && AuditLogs.Count == 0;

        /// <summary>
        /// 是否可以上一页
        /// </summary>
        public bool CanGoPrevious => CurrentPage > 1;

        /// <summary>
        /// 是否可以下一页
        /// </summary>
        public bool CanGoNext => CurrentPage < TotalPages;

        #endregion

        #region 构造函数

        public EntityAuditLogDialogViewModel(
            IApiService apiService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(loggerFactory, eventAggregator)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            Title = "变更记录";
        }

        #endregion

        #region 对话框生命周期

        /// <summary>
        /// 对话框打开时处理参数
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters == null) return;

            EntityType = GetDialogParameter(parameters, "EntityType", string.Empty);
            EntityId = GetDialogParameter(parameters, "EntityId", Guid.Empty);
            EntityDescription = GetDialogParameter(parameters, "EntityDescription", string.Empty);

            // 根据实体类型设置标题
            Title = GetTitleByEntityType(EntityType);

            Logger.LogInformation("EntityAuditLogDialog - 打开对话框，EntityType: {EntityType}, EntityId: {EntityId}",
                EntityType, EntityId);

            // 加载审计日志
            _ = LoadAuditLogsAsync();
        }

        /// <summary>
        /// 对话框关闭时清理
        /// </summary>
        protected override void OnDialogClosedCore()
        {
            Logger.LogInformation("EntityAuditLogDialog - 对话框已关闭");
        }

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAuditLogsAsync();
        }

        /// <summary>
        /// 关闭命令
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadAuditLogsAsync();
            }
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadAuditLogsAsync();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载审计日志
        /// </summary>
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
                OnPropertyChanged(nameof(ShowEmptyMessage));
            }
        }

        /// <summary>
        /// 根据实体类型获取标题
        /// </summary>
        private static string GetTitleByEntityType(string entityType)
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
