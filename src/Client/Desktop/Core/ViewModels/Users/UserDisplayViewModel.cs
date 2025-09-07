using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Users
{

    /// <summary>
    /// 用户显示视图模型 - UltraThink架构的展示层
    /// 负责UI展示逻辑、格式化和业务规则判断
    /// </summary>
    public class UserDisplayViewModel : BindableBase
    {

        #region Fields

        private UserDto _userData;

        #endregion Fields

        #region Constructor

        public UserDisplayViewModel(UserDto userData)
        {
            _userData = userData ?? throw new ArgumentNullException(nameof(userData));
        }

        #endregion Constructor

        #region Data Properties

        /// <summary>用户数据</summary>
        public UserDto UserData
        {
            get => _userData;
            set => SetProperty(ref _userData, value);
        }

        #endregion Data Properties

        #region Display Properties

        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrEmpty(_userData.RealName) ? _userData.Username : _userData.RealName;

        /// <summary>完整显示名称（含用户名）</summary>
        public string FullDisplayName => string.IsNullOrEmpty(_userData.RealName)
            ? _userData.Username
            : $"{_userData.RealName}（{_userData.Username}）";

        /// <summary>状态文本</summary>
        public string StatusText => _userData.Status.GetDescription();

        /// <summary>角色文本</summary>
        public string RoleText => ((Enum)(object)_userData.Role).GetDescription();

        /// <summary>创建时间显示文本</summary>
        public string CreateTimeText => "N/A"; // UltraThink v2.0简化：CreateTime字段已删除

        /// <summary>更新时间显示文本</summary>
        public string UpdateTimeText => "N/A"; // UltraThink v2.0简化：UpdateTime字段已删除

        /// <summary>最后登录时间显示文本</summary>
        public string LastLoginTimeText => "N/A"; // UltraThink v2.0简化：LastLoginTime字段已删除

        /// <summary>联系信息显示文本</summary>
        public string ContactText
        {
            get
            {
                var contact = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(_userData.PhoneNumber))
                {
                    contact.Add($"电话: {_userData.PhoneNumber}");
                }

                if (!string.IsNullOrEmpty(_userData.Email))
                {
                    contact.Add($"邮箱: {_userData.Email}");
                }

                return contact.Count > 0 ? string.Join(" | ", contact) : "无联系方式";
            }
        }

        #endregion Display Properties

        #region UI Business Rules

        /// <summary>是否为系统管理员</summary>
        public bool IsSysAdmin => _userData.Username == "sysadmin";

        /// <summary>是否可以编辑</summary>
        public bool CanEdit => _userData.Status == CommonStatus.Enabled && !IsSysAdmin;

        /// <summary>是否可以删除</summary>
        public bool CanDelete => !IsSysAdmin && _userData.Status != CommonStatus.Enabled;

        /// <summary>是否可以重置密码</summary>
        public bool CanResetPassword => _userData.Status == CommonStatus.Enabled;

        /// <summary>是否活跃用户</summary>
        public bool IsActive => _userData.Status == CommonStatus.Enabled;

        /// <summary>是否可以启用</summary>
        public bool CanEnable => _userData.Status == CommonStatus.Disabled && !IsSysAdmin;

        /// <summary>是否可以禁用</summary>
        public bool CanDisable => _userData.Status == CommonStatus.Enabled && !IsSysAdmin;

        /// <summary>是否为新用户（创建时间小于24小时）</summary>
        public bool IsNewUser => false; // UltraThink v2.0简化：CreateTime字段已删除，无法判断

        /// <summary>是否长时间未登录（超过30天）</summary>
        public bool IsLongTimeNoLogin => false; // UltraThink v2.0简化：LastLoginTime字段已删除，无法判断

        #endregion UI Business Rules

        #region Update Methods

        /// <summary>
        /// 更新用户数据并刷新所有相关属性
        /// </summary>
        public void UpdateUserData(UserDto newUserData)
        {
            UserData = newUserData;

            // 刷新所有计算属性
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(FullDisplayName));
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(RoleText));
            RaisePropertyChanged(nameof(CreateTimeText));
            RaisePropertyChanged(nameof(UpdateTimeText));
            RaisePropertyChanged(nameof(LastLoginTimeText));
            RaisePropertyChanged(nameof(ContactText));
            RaisePropertyChanged(nameof(IsSysAdmin));
            RaisePropertyChanged(nameof(CanEdit));
            RaisePropertyChanged(nameof(CanDelete));
            RaisePropertyChanged(nameof(CanResetPassword));
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(CanEnable));
            RaisePropertyChanged(nameof(CanDisable));
            RaisePropertyChanged(nameof(IsNewUser));
            RaisePropertyChanged(nameof(IsLongTimeNoLogin));
        }

        #endregion Update Methods
    }
}
