using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Users
{

    /// <summary>
    /// 用户主题视图模型 - UltraThink架构的主题层
    /// 负责UI主题、颜色和样式相关逻辑
    /// </summary>
    public class UserThemeViewModel : BindableBase
    {

        #region Fields

        private UserDto _userData;

        #endregion Fields

        #region Constructor

        public UserThemeViewModel(UserDto userData)
        {
            _userData = userData ?? throw new System.ArgumentNullException(nameof(userData));
        }

        #endregion Constructor

        #region Theme Colors

        /// <summary>状态颜色</summary>
        public string StatusColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#4CAF50",    // 绿色 - 启用
            CommonStatus.Disabled => "#F44336",   // 红色 - 禁用
            _ => "#9E9E9E" // 灰色 - 未知
        };

        /// <summary>状态颜色画刷</summary>
        public SolidColorBrush StatusBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(StatusColor));

        /// <summary>角色颜色</summary>
        public string RoleColor => _userData.Role?.ToLower() switch
        {
            "admin" => "#FF9800",          // 橙色 - 管理员
            "doctor" => "#2196F3",         // 蓝色 - 医生
            "pharmacist" => "#4CAF50",     // 绿色 - 药师
            "receptionist" => "#FF5722",   // 红色 - 前台
            "cashier" => "#795548",        // 棕色 - 收银员
            "therapist" => "#9C27B0",      // 紫色 - 理疗师
            _ => "#9E9E9E" // 灰色 - 未知
        };

        /// <summary>角色颜色画刷</summary>
        public SolidColorBrush RoleBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(RoleColor));

        /// <summary>背景颜色（基于状态）</summary>
        public string BackgroundColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#FFFFFF",    // 白色 - 正常
            CommonStatus.Disabled => "#F5F5F5",   // 浅灰色 - 禁用
            _ => "#FAFAFA" // 极浅灰色 - 其他
        };

        /// <summary>背景颜色画刷</summary>
        public SolidColorBrush BackgroundBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(BackgroundColor));

        /// <summary>边框颜色</summary>
        public string BorderColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#E0E0E0",    // 浅灰色边框
            CommonStatus.Disabled => "#BDBDBD",   // 中灰色边框
            _ => "#EEEEEE" // 极浅灰色边框
        };

        /// <summary>边框颜色画刷</summary>
        public SolidColorBrush BorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(BorderColor));

        #endregion Theme Colors

        #region Text Colors

        /// <summary>主文本颜色</summary>
        public string PrimaryTextColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#212121",    // 深灰色 - 正常
            CommonStatus.Disabled => "#9E9E9E",   // 中灰色 - 禁用
            _ => "#757575" // 灰色 - 其他
        };

        /// <summary>主文本颜色画刷</summary>
        public SolidColorBrush PrimaryTextBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(PrimaryTextColor));

        /// <summary>次要文本颜色</summary>
        public string SecondaryTextColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#757575",    // 中灰色
            CommonStatus.Disabled => "#BDBDBD",   // 浅灰色
            _ => "#9E9E9E" // 灰色
        };

        /// <summary>次要文本颜色画刷</summary>
        public SolidColorBrush SecondaryTextBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(SecondaryTextColor));

        #endregion Text Colors

        #region Icon Colors

        /// <summary>图标颜色</summary>
        public string IconColor => _userData.Status switch
        {
            CommonStatus.Enabled => "#616161",    // 中深灰色
            CommonStatus.Disabled => "#BDBDBD",   // 浅灰色
            _ => "#9E9E9E" // 灰色
        };

        /// <summary>图标颜色画刷</summary>
        public SolidColorBrush IconBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IconColor));

        /// <summary>状态图标颜色（与状态颜色一致）</summary>
        public string StatusIconColor => StatusColor;

        /// <summary>状态图标画刷</summary>
        public SolidColorBrush StatusIconBrush => StatusBrush;

        #endregion Icon Colors

        #region Opacity Values

        /// <summary>内容透明度</summary>
        public double ContentOpacity => _userData.Status switch
        {
            CommonStatus.Enabled => 1.0,          // 完全不透明
            CommonStatus.Disabled => 0.6,         // 半透明
            _ => 0.8 // 稍微透明
        };

        /// <summary>图标透明度</summary>
        public double IconOpacity => _userData.Status switch
        {
            CommonStatus.Enabled => 0.87,         // Material Design 标准
            CommonStatus.Disabled => 0.38,        // Material Design 禁用状态
            _ => 0.6 // 中等透明度
        };

        #endregion Opacity Values

        #region Style Names

        /// <summary>状态样式名称</summary>
        public string StatusStyleName => $"User{_userData.Status}Style";

        /// <summary>角色样式名称</summary>
        public string RoleStyleName => $"User{_userData.Role.ToString()}Style";

        /// <summary>组合样式名称</summary>
        public string CompositeStyleName => $"User{_userData.Role.ToString()}{_userData.Status.ToString()}Style";

        #endregion Style Names

        #region Update Methods

        /// <summary>
        /// 更新用户数据并刷新主题相关属性
        /// </summary>
        public void UpdateUserData(UserDto newUserData)
        {
            _userData = newUserData;

            // 刷新所有主题相关属性
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(StatusBrush));
            RaisePropertyChanged(nameof(RoleColor));
            RaisePropertyChanged(nameof(RoleBrush));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(BackgroundBrush));
            RaisePropertyChanged(nameof(BorderColor));
            RaisePropertyChanged(nameof(BorderBrush));
            RaisePropertyChanged(nameof(PrimaryTextColor));
            RaisePropertyChanged(nameof(PrimaryTextBrush));
            RaisePropertyChanged(nameof(SecondaryTextColor));
            RaisePropertyChanged(nameof(SecondaryTextBrush));
            RaisePropertyChanged(nameof(IconColor));
            RaisePropertyChanged(nameof(IconBrush));
            RaisePropertyChanged(nameof(StatusIconColor));
            RaisePropertyChanged(nameof(StatusIconBrush));
            RaisePropertyChanged(nameof(ContentOpacity));
            RaisePropertyChanged(nameof(IconOpacity));
            RaisePropertyChanged(nameof(StatusStyleName));
            RaisePropertyChanged(nameof(RoleStyleName));
            RaisePropertyChanged(nameof(CompositeStyleName));
        }

        #endregion Update Methods
    }
}
