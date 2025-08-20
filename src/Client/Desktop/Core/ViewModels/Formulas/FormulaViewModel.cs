using System;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Formulas
{
    /// <summary>
    /// 验方视图模型协调器 - UltraThink架构Presentation Layer
    /// 组合Display、State、Theme等专门的ViewModel，实现完整的验方UI逻辑
    /// </summary>
    public class FormulaViewModel : BindableBase
    {
        private readonly FormulaDto _formulaData;

        #region 子ViewModel组件

        /// <summary>显示逻辑组件</summary>
        public FormulaDisplayViewModel Display { get; }

        /// <summary>状态管理组件</summary>
        public FormulaStateViewModel State { get; }

        /// <summary>主题样式组件</summary>
        public FormulaThemeViewModel Theme { get; }

        #endregion

        #region 构造函数

        private FormulaViewModel(FormulaDto formulaData)
        {
            _formulaData = formulaData ?? throw new ArgumentNullException(nameof(formulaData));
            
            Display = new FormulaDisplayViewModel(_formulaData);
            State = new FormulaStateViewModel();
            Theme = new FormulaThemeViewModel(_formulaData);
            
            // 监听状态变化
            State.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
        }

        #endregion

        #region 工厂方法

        /// <summary>
        /// 创建验方视图模型
        /// </summary>
        public static FormulaViewModel Create(FormulaDto formulaData)
        {
            return new FormulaViewModel(formulaData);
        }

        #endregion

        #region 数据访问属性

        /// <summary>验方ID</summary>
        public Guid Id => _formulaData.Id;

        /// <summary>验方名称</summary>
        public string Name => _formulaData.Name ?? string.Empty;

        /// <summary>访问原始验方数据</summary>
        public FormulaDto FormulaData => _formulaData;

        #endregion

        #region 快捷访问属性

        /// <summary>显示名称</summary>
        public string DisplayName => Display.DisplayName;

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => State.IsSelected;
            set => State.IsSelected = value;
        }

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => State.IsExpanded;
            set => State.IsExpanded = value;
        }

        /// <summary>是否正在编辑</summary>
        public bool IsEditing
        {
            get => State.IsEditing;
            set => State.IsEditing = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => State.IsLoading;
            set => State.IsLoading = value;
        }

        /// <summary>是否有错误</summary>
        public bool HasError => State.HasError;

        /// <summary>错误消息</summary>
        public string ErrorMessage => State.ErrorMessage;

        /// <summary>总价格（计算属性）</summary>
        public decimal TotalPrice => _formulaData.TotalPrice;

        /// <summary>药材数量</summary>
        public int HerbCount => _formulaData.HerbCount;

        /// <summary>药材名称列表</summary>
        public string HerbNames => _formulaData.HerbNames;

        /// <summary>验方分类</summary>
        public string Category => _formulaData.Category;

        /// <summary>是否共享</summary>
        public bool IsShared => _formulaData.IsShared;

        /// <summary>功效</summary>
        public string Effect => _formulaData.Effect ?? string.Empty;

        /// <summary>用法</summary>
        public string Usage => _formulaData.Usage ?? string.Empty;

        /// <summary>备注</summary>
        public string Remark => _formulaData.Remark ?? string.Empty;

        #endregion

        #region 业务方法

        /// <summary>
        /// 开始编辑
        /// </summary>
        public void StartEdit()
        {
            State.StartEditing();
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        public void CancelEdit()
        {
            State.EndEditing();
            State.ClearError();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            State.ToggleSelection();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string message)
        {
            State.SetError(message);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            State.ClearError();
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void ResetState()
        {
            State.ResetState();
        }

        /// <summary>
        /// 获取限制数量的药材名称列表
        /// </summary>
        public string GetHerbNamesList(int maxCount = 10)
        {
            return _formulaData.GetHerbNamesList(maxCount);
        }

        /// <summary>
        /// 检查是否可以导出
        /// </summary>
        public bool CanExport()
        {
            return !HasError && HerbCount > 0;
        }

        /// <summary>
        /// 检查是否适合导入（验证数据完整性）
        /// </summary>
        public bool IsValidForImport()
        {
            return !string.IsNullOrWhiteSpace(Name) && 
                   HerbCount > 0 && 
                   !string.IsNullOrWhiteSpace(Effect);
        }

        /// <summary>
        /// 格式化价格显示
        /// </summary>
        public string FormatPrice()
        {
            return TotalPrice.ToString("F2") + "元";
        }

        /// <summary>
        /// 格式化药材组成摘要
        /// </summary>
        public string GetCompositionSummary()
        {
            return $"{HerbCount}味药材，总价 {FormatPrice()}";
        }

        #endregion

        #region 数据更新

        /// <summary>
        /// 通知数据已更新（用于刷新显示）
        /// </summary>
        public void NotifyDataUpdated()
        {
            // 通知所有显示相关属性更新
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(Name));
            
            // 让子组件也更新 (受保护成员访问错误，暂时注释)
            // Display.RaisePropertyChanged(string.Empty); // 无法访问受保护成员：FormulaDisplayViewModel.RaisePropertyChanged
            // Theme.RaisePropertyChanged(string.Empty); // 无法访问受保护成员：FormulaThemeViewModel.RaisePropertyChanged
        }

        #endregion

        #region 比较和相等性

        public override bool Equals(object? obj)
        {
            if (obj is FormulaViewModel other)
                return Id.Equals(other.Id);
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"FormulaViewModel: {DisplayName} (ID: {Id})";
        }

        #endregion
    }
}