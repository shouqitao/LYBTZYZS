using System;
using System.ComponentModel;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase
{
    /// <summary>
    /// 医疗案例协调视图模型 - UltraThink架构Business Layer
    /// 组合Display、State、Theme三个ViewModel，实现完整的医疗案例视图逻辑
    /// 遵循单一职责原则和关注点分离
    /// </summary>
    public class MedicalCaseViewModel : BindableBase
    {
        #region Fields

        private readonly MedicalCaseDto _medicalCaseData;
        private readonly MedicalCaseDisplayViewModel _display;
        private readonly MedicalCaseStateViewModel _state;
        private readonly MedicalCaseThemeViewModel _theme;

        #endregion

        #region Constructor

        private MedicalCaseViewModel(MedicalCaseDto medicalCaseData)
        {
            _medicalCaseData = medicalCaseData ?? throw new ArgumentNullException(nameof(medicalCaseData));
            
            // 初始化三个专门的ViewModel
            _display = new MedicalCaseDisplayViewModel(_medicalCaseData);
            _state = new MedicalCaseStateViewModel();
            _theme = new MedicalCaseThemeViewModel(_medicalCaseData);

            // 监听状态变化以便通知UI更新
            _state.PropertyChanged += OnStatePropertyChanged;
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// 创建医疗案例视图模型实例
        /// </summary>
        public static MedicalCaseViewModel Create(MedicalCaseDto medicalCaseData)
        {
            return new MedicalCaseViewModel(medicalCaseData);
        }

        #endregion

        #region Core Properties

        /// <summary>医疗案例业务数据（只读）</summary>
        public MedicalCaseDto MedicalCaseData => _medicalCaseData;

        /// <summary>显示逻辑视图模型</summary>
        public MedicalCaseDisplayViewModel Display => _display;

        /// <summary>状态管理视图模型</summary>
        public MedicalCaseStateViewModel State => _state;

        /// <summary>主题样式视图模型</summary>
        public MedicalCaseThemeViewModel Theme => _theme;

        #endregion

        #region Convenience Properties

        /// <summary>医疗案例ID</summary>
        public Guid Id => _medicalCaseData.Id;

        /// <summary>患者ID</summary>
        public Guid PatientId => _medicalCaseData.PatientId;

        /// <summary>医生ID</summary>
        public Guid DoctorId => _medicalCaseData.DoctorId;

        /// <summary>患者姓名显示</summary>
        public string PatientName => _display.PatientNameDisplay;

        /// <summary>医生姓名显示</summary>
        public string DoctorName => _display.DoctorNameDisplay;

        /// <summary>显示名称（用于列表显示）</summary>
        public string DisplayName => $"{PatientName} - {_display.StatusDisplay}";

        /// <summary>状态显示</summary>
        public string StatusDisplay => _display.StatusDisplay;

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => _display.CreateTimeDisplay;

        /// <summary>案例摘要</summary>
        public string CaseSummary => $"{_medicalCaseData.PatientName} - {_medicalCaseData.CaseStatus}"; // UltraThink v2.0简化：直接组合显示信息

        #endregion

        #region State Convenience Properties

        /// <summary>是否被选中</summary>
        public bool IsSelected
        {
            get => _state.IsSelected;
            set => _state.IsSelected = value;
        }

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => _state.IsExpanded;
            set => _state.IsExpanded = value;
        }

        /// <summary>是否正在编辑</summary>
        public bool IsEditing
        {
            get => _state.IsEditing;
            set => _state.IsEditing = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _state.IsLoading;
            set => _state.IsLoading = value;
        }

        /// <summary>是否有错误</summary>
        public bool HasError
        {
            get => _state.HasError;
            set => _state.HasError = value;
        }

        /// <summary>错误消息</summary>
        public string ErrorMessage
        {
            get => _state.ErrorMessage;
            set => _state.ErrorMessage = value;
        }

        /// <summary>是否高亮显示</summary>
        public bool IsHighlighted
        {
            get => _state.IsHighlighted;
            set => _state.IsHighlighted = value;
        }

        #endregion

        #region Medical Case Specific State

        /// <summary>是否正在开始看诊</summary>
        public bool IsStartingConsultation
        {
            get => _state.IsStartingConsultation;
            set => _state.IsStartingConsultation = value;
        }

        /// <summary>是否正在完成案例</summary>
        public bool IsCompleting
        {
            get => _state.IsCompleting;
            set => _state.IsCompleting = value;
        }

        /// <summary>是否正在取消案例</summary>
        public bool IsCancelling
        {
            get => _state.IsCancelling;
            set => _state.IsCancelling = value;
        }

        /// <summary>是否正在删除</summary>
        public bool IsDeleting
        {
            get => _state.IsDeleting;
            set => _state.IsDeleting = value;
        }

        #endregion

        #region Business Logic Convenience Methods

        /// <summary>
        /// 开始编辑案例
        /// </summary>
        public void StartEditing()
        {
            _state.StartEditing();
        }

        /// <summary>
        /// 结束编辑案例
        /// </summary>
        public void EndEditing()
        {
            _state.EndEditing();
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public void StartStartingConsultation()
        {
            _state.StartStartingConsultation();
        }

        /// <summary>
        /// 结束开始看诊
        /// </summary>
        public void EndStartingConsultation()
        {
            _state.EndStartingConsultation();
        }

        /// <summary>
        /// 开始完成案例
        /// </summary>
        public void StartCompleting()
        {
            _state.StartCompleting();
        }

        /// <summary>
        /// 结束完成案例
        /// </summary>
        public void EndCompleting()
        {
            _state.EndCompleting();
        }

        /// <summary>
        /// 开始取消案例
        /// </summary>
        public void StartCancelling()
        {
            _state.StartCancelling();
        }

        /// <summary>
        /// 结束取消案例
        /// </summary>
        public void EndCancelling()
        {
            _state.EndCancelling();
        }

        /// <summary>
        /// 开始删除
        /// </summary>
        public void StartDeleting()
        {
            _state.StartDeleting();
        }

        /// <summary>
        /// 结束删除
        /// </summary>
        public void EndDeleting()
        {
            _state.EndDeleting();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string message)
        {
            _state.SetError(message);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            _state.ClearError();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            _state.ToggleSelection();
        }

        /// <summary>
        /// 切换展开状态
        /// </summary>
        public void ToggleExpansion()
        {
            _state.ToggleExpansion();
        }

        /// <summary>
        /// 设置为焦点状态
        /// </summary>
        public void SetFocus()
        {
            _state.SetFocus();
        }

        /// <summary>
        /// 取消焦点状态
        /// </summary>
        public void ClearFocus()
        {
            _state.ClearFocus();
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void ResetState()
        {
            _state.ResetState();
        }

        #endregion

        #region Display Convenience Methods

        /// <summary>
        /// 获取案例摘要信息
        /// </summary>
        public string GetSummaryInfo()
        {
            return _display.GetSummaryInfo();
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            return _display.GetDetailedInfo();
        }

        /// <summary>
        /// 获取打印用格式化文本
        /// </summary>
        public string GetPrintableInfo()
        {
            return _display.GetPrintableInfo();
        }

        /// <summary>
        /// 获取状态徽章文本
        /// </summary>
        public string GetStatusBadge()
        {
            return _display.GetStatusBadge();
        }

        /// <summary>
        /// 获取进度显示
        /// </summary>
        public string GetProgressDisplay()
        {
            return _display.GetProgressDisplay();
        }

        /// <summary>
        /// 获取操作建议
        /// </summary>
        public string GetActionSuggestion()
        {
            return _display.GetActionSuggestion();
        }

        #endregion

        #region Business Data Convenience Methods

        /// <summary>
        /// 检查案例是否包含指定关键字
        /// </summary>
        public bool ContainsKeyword(string keyword)
        {
            // UltraThink v2.0简化：基础字符串搜索，移除扩展方法依赖
            if (string.IsNullOrWhiteSpace(keyword)) return true;
            return _medicalCaseData.PatientName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                   _medicalCaseData.DoctorName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                   _medicalCaseData.Remark?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// 检查案例是否已完成
        /// </summary>
        public bool IsCompleted => _medicalCaseData.CaseStatus == MedicalCaseStatus.Completed; // UltraThink v2.0简化：基于状态直接判断

        /// <summary>
        /// 检查案例是否进行中
        /// </summary>
        public bool IsInProgress => _medicalCaseData.CaseStatus == MedicalCaseStatus.InConsultation; // UltraThink v2.0简化：基于状态直接判断

        /// <summary>
        /// 检查案例是否已取消
        /// </summary>
        public bool IsCancelled => _medicalCaseData.CaseStatus == MedicalCaseStatus.Cancelled; // UltraThink v2.0简化：基于状态直接判断

        /// <summary>
        /// 检查案例是否为新创建
        /// </summary>
        public bool IsNew => _medicalCaseData.CaseStatus == MedicalCaseStatus.Registered; // UltraThink v2.0简化：基于状态直接判断

        /// <summary>
        /// 检查案例是否紧急
        /// </summary>
        public bool IsUrgent => _medicalCaseData.IsUrgent();

        /// <summary>
        /// 检查案例是否为当日案例
        /// </summary>
        public bool IsToday => _medicalCaseData.ConsultationDate.Date == DateTime.Today; // UltraThink v2.0简化：直接比较日期

        /// <summary>
        /// 检查是否可以开始看诊
        /// </summary>
        public bool CanStartConsultation => _medicalCaseData.CanStartConsultation() && _state.CanStartConsultation; // UltraThink v2.0简化：使用DTO现有方法

        /// <summary>
        /// 检查是否可以完成案例
        /// </summary>
        public bool CanComplete => _medicalCaseData.CaseStatus == MedicalCaseStatus.InConsultation && _state.CanComplete; // UltraThink v2.0简化：基于状态判断

        /// <summary>
        /// 检查是否可以取消案例
        /// </summary>
        public bool CanCancel => _medicalCaseData.CaseStatus != MedicalCaseStatus.Completed && _state.CanCancel; // UltraThink v2.0简化：基于状态判断

        /// <summary>
        /// 检查是否可以删除
        /// </summary>
        public bool CanDelete => _medicalCaseData.CaseStatus != MedicalCaseStatus.Completed && _state.CanDelete; // UltraThink v2.0简化：基于状态判断

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        public bool CanEdit => _medicalCaseData.CaseStatus != MedicalCaseStatus.Completed && _state.CanEdit; // UltraThink v2.0简化：基于状态判断

        /// <summary>
        /// 获取案例持续时间（分钟）
        /// </summary>
        public double GetDurationInMinutes()
        {
            // UltraThink v2.0简化：基于看诊时间计算，移除扩展方法依赖
            return (DateTime.Now - _medicalCaseData.ConsultationDate).TotalMinutes;
        }

        /// <summary>
        /// 获取案例优先级
        /// </summary>
        public string GetPriority()
        {
            // UltraThink v2.0简化：基于状态和时间判断，返回字符串而非整数
            var priority = _medicalCaseData.GetPriority();
            return priority switch
            {
                3 => "高",
                2 => "中",
                1 => "低",
                _ => "正常"
            };
        }

        /// <summary>
        /// 检查是否需要医生关注
        /// </summary>
        public bool NeedsDoctorAttention => _medicalCaseData.NeedsDoctorAttention();

        #endregion

        #region Theme Convenience Methods

        /// <summary>
        /// 更新时间相关主题
        /// </summary>
        public void UpdateTimeBasedTheme()
        {
            _theme.UpdateTimeBasedTheme();
        }

        /// <summary>
        /// 切换高对比度模式
        /// </summary>
        public void ToggleHighContrastMode()
        {
            _theme.IsHighContrastMode = !_theme.IsHighContrastMode;
        }

        #endregion

        #region Event Handling

        private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当State的属性改变时，通知相关的便利属性也已改变
            switch (e.PropertyName)
            {
                case nameof(MedicalCaseStateViewModel.IsSelected):
                    RaisePropertyChanged(nameof(IsSelected));
                    break;
                case nameof(MedicalCaseStateViewModel.IsExpanded):
                    RaisePropertyChanged(nameof(IsExpanded));
                    break;
                case nameof(MedicalCaseStateViewModel.IsEditing):
                    RaisePropertyChanged(nameof(IsEditing));
                    break;
                case nameof(MedicalCaseStateViewModel.IsLoading):
                    RaisePropertyChanged(nameof(IsLoading));
                    break;
                case nameof(MedicalCaseStateViewModel.HasError):
                    RaisePropertyChanged(nameof(HasError));
                    break;
                case nameof(MedicalCaseStateViewModel.ErrorMessage):
                    RaisePropertyChanged(nameof(ErrorMessage));
                    break;
                case nameof(MedicalCaseStateViewModel.IsHighlighted):
                    RaisePropertyChanged(nameof(IsHighlighted));
                    break;
                case nameof(MedicalCaseStateViewModel.IsStartingConsultation):
                    RaisePropertyChanged(nameof(IsStartingConsultation));
                    RaisePropertyChanged(nameof(CanStartConsultation));
                    break;
                case nameof(MedicalCaseStateViewModel.IsCompleting):
                    RaisePropertyChanged(nameof(IsCompleting));
                    RaisePropertyChanged(nameof(CanComplete));
                    break;
                case nameof(MedicalCaseStateViewModel.IsCancelling):
                    RaisePropertyChanged(nameof(IsCancelling));
                    RaisePropertyChanged(nameof(CanCancel));
                    break;
                case nameof(MedicalCaseStateViewModel.IsDeleting):
                    RaisePropertyChanged(nameof(IsDeleting));
                    RaisePropertyChanged(nameof(CanDelete));
                    break;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            // UltraThink v2.0简化：基础验证逻辑，移除扩展方法依赖
            if (string.IsNullOrWhiteSpace(_medicalCaseData.PatientName))
                return (false, "患者姓名不能为空");
            if (string.IsNullOrWhiteSpace(_medicalCaseData.DoctorName))
                return (false, "医生姓名不能为空");
            if (_medicalCaseData.ConsultationDate > DateTime.Now.AddDays(1))
                return (false, "看诊时间不能超过明日");
            return (true, null);
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return $"MedicalCaseViewModel: {DisplayName} ({StatusDisplay})";
        }

        public override bool Equals(object? obj)
        {
            return obj is MedicalCaseViewModel other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消事件订阅
                    _state.PropertyChanged -= OnStatePropertyChanged;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}