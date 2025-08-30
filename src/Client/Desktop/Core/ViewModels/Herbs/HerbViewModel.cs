using System;
// UltraThink v2.0: 使用HerbDto替代HerbInfo模型
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Extensions;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Herbs
{
    /// <summary>
    /// 药材视图模型 - UltraThink架构的协调层
    /// 协调数据、显示、状态和主题四个关注点
    /// 实现了完全的关注点分离
    /// </summary>
    public class HerbViewModel : BindableBase
    {
        #region Fields

        private HerbDisplayViewModel _display;
        private HerbStateViewModel _state;
        private HerbThemeViewModel _theme;

        #endregion

        #region Constructor

        public HerbViewModel(HerbDto herbData)
        {
            if (herbData == null)
                throw new ArgumentNullException(nameof(herbData));

            _display = new HerbDisplayViewModel(herbData);
            _state = new HerbStateViewModel();
            _theme = new HerbThemeViewModel(herbData);
        }

        #endregion

        #region Component ViewModels

        /// <summary>显示逻辑视图模型</summary>
        public HerbDisplayViewModel Display
        {
            get => _display;
            private set => SetProperty(ref _display, value);
        }

        /// <summary>UI状态视图模型</summary>
        public HerbStateViewModel State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        /// <summary>主题样式视图模型</summary>
        public HerbThemeViewModel Theme
        {
            get => _theme;
            private set => SetProperty(ref _theme, value);
        }

        #endregion

        #region Convenient Properties

        /// <summary>药材数据（只读）</summary>
        public HerbDto HerbData => Display.HerbData;

        /// <summary>药材ID</summary>
        public Guid Id => HerbData.Id;

        /// <summary>药材名称</summary>
        public string Name => HerbData.Name;

        /// <summary>显示名称</summary>
        public string DisplayName => Display.DisplayName;

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => State.IsSelected;
            set => State.IsSelected = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => State.IsLoading;
            set
            {
                if (value)
                    State.StartLoading();
                else
                    State.StopLoading();
            }
        }

        /// <summary>是否编辑中</summary>
        public bool IsEditing
        {
            get => State.IsEditing;
            set
            {
                if (value)
                    State.StartEditing();
                else
                    State.StopEditing();
            }
        }

        /// <summary>是否展开详情</summary>
        public bool IsExpanded
        {
            get => State.IsExpanded;
            set => State.IsExpanded = value;
        }

        /// <summary>价格</summary>
        public decimal Price => HerbData.Price;

        /// <summary>是否启用</summary>
        public bool IsEnabled => HerbData.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled; // UltraThink v2.0简化：使用CommonStatus替代HerbStatus

        #endregion

        #region Update Methods

        /// <summary>
        /// 更新药材数据
        /// </summary>
        public void UpdateHerbData(HerbDto newHerbData)
        {
            if (newHerbData == null)
                throw new ArgumentNullException(nameof(newHerbData));

            Display.UpdateHerbData(newHerbData);
            Theme.UpdateHerbData(newHerbData);

            // 通知相关属性变化
            RaisePropertyChanged(nameof(HerbData));
            RaisePropertyChanged(nameof(Id));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(Price));
            RaisePropertyChanged(nameof(IsEnabled));
        }

        /// <summary>
        /// 开始编辑模式
        /// </summary>
        public void StartEditing()
        {
            State.StartEditing();
        }

        /// <summary>
        /// 结束编辑模式
        /// </summary>
        public void StopEditing()
        {
            State.StopEditing();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            State.ToggleSelection();
        }

        /// <summary>
        /// 切换展开状态
        /// </summary>
        public void ToggleExpanded()
        {
            State.ToggleExpanded();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string errorMessage)
        {
            State.SetError(errorMessage);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            State.ClearError();
        }

        /// <summary>
        /// 重置UI状态
        /// </summary>
        public void ResetState()
        {
            State.Reset();
        }

        /// <summary>
        /// 重置选择状态
        /// </summary>
        public void ResetSelection()
        {
            State.ResetSelection();
        }

        #endregion

        #region Business Operations

        /// <summary>
        /// 计算指定数量的总价
        /// </summary>
        public decimal CalculateTotalPrice(decimal quantity)
        {
            return HerbData.Price * quantity;
        }

        /// <summary>
        /// 获取价格等级 - 用于UI主题显示
        /// </summary>
        public int GetPriceLevel()
        {
            if (Price <= 10) return 1; // 低价
            if (Price <= 50) return 2; // 中价
            return 3; // 高价
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// 处理鼠标进入事件
        /// </summary>
        public void OnMouseEnter()
        {
            State.OnMouseEnter();
        }

        /// <summary>
        /// 处理鼠标离开事件
        /// </summary>
        public void OnMouseLeave()
        {
            State.OnMouseLeave();
        }

        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnClick()
        {
            State.OnClick();
        }

        /// <summary>
        /// 处理双击事件
        /// </summary>
        public void OnDoubleClick()
        {
            State.OnDoubleClick();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建药材视图模型
        /// </summary>
        public static HerbViewModel Create(HerbDto herbData)
        {
            return new HerbViewModel(herbData);
        }

        /// <summary>
        /// 从现有药材视图模型更新数据
        /// </summary>
        public static HerbViewModel UpdateFrom(HerbViewModel existingViewModel, HerbDto newHerbData)
        {
            existingViewModel.UpdateHerbData(newHerbData);
            return existingViewModel;
        }

        // UltraThink v2.0: HerbInfo模型已被移除，此方法不再需要
        // /// <summary>
        // /// 从 HerbInfo 创建（已废弃，请使用DTO直接创建）
        // /// </summary>
        // public static HerbViewModel CreateFromHerbInfo(HerbInfo herbInfo)
        // {
        //     var herbData = HerbDto.FromHerbInfo(herbInfo);
        //     return new HerbViewModel(herbData);
        // }

        #endregion

        #region Equality and Comparison

        /// <summary>
        /// 判断是否为同一药材
        /// </summary>
        public bool IsSameHerb(HerbViewModel other)
        {
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// 判断是否为同一药材（通过药材数据）
        /// </summary>
        public bool IsSameHerb(HerbDto herbData)
        {
            return herbData != null && Id == herbData.Id;
        }

        /// <summary>
        /// 判断是否为同一药材（通过名称和规格）
        /// </summary>
        public bool IsSameHerbByNameAndSpec(HerbViewModel other)
        {
            // UltraThink v2.0简化：直接比较名称和规格，移除扩展方法依赖
            return other != null && 
                   string.Equals(HerbData.Name, other.HerbData.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(HerbData.Spec, other.HerbData.Spec, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is HerbViewModel other && IsSameHerb(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region String Representation

        public override string ToString()
        {
            return $"HerbViewModel: {Name} (ID: {Id})";
        }

        #endregion
    }
}