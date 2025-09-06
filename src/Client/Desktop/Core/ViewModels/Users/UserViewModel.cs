using LYBT.Shared.Models.Contracts.Users;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Users {

    /// <summary>
    /// 用户视图模型 - UltraThink架构的协调层
    /// 协调数据、显示、状态和主题四个关注点
    /// 实现了完全的关注点分离
    /// </summary>
    public class UserViewModel : BindableBase {

        #region Fields

        private UserDisplayViewModel _display;
        private UserStateViewModel _state;
        private UserThemeViewModel _theme;

        #endregion Fields

        #region Constructor

        public UserViewModel(UserDto userData) {
            if (userData == null) {
                throw new ArgumentNullException(nameof(userData));
            }

            _display = new UserDisplayViewModel(userData);
            _state = new UserStateViewModel();
            _theme = new UserThemeViewModel(userData);
        }

        #endregion Constructor

        #region Component ViewModels

        /// <summary>显示逻辑视图模型</summary>
        public UserDisplayViewModel Display {
            get => _display;
            private set => SetProperty(ref _display, value);
        }

        /// <summary>UI状态视图模型</summary>
        public UserStateViewModel State {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        /// <summary>主题样式视图模型</summary>
        public UserThemeViewModel Theme {
            get => _theme;
            private set => SetProperty(ref _theme, value);
        }

        #endregion Component ViewModels

        #region Convenient Properties

        /// <summary>用户数据（只读）</summary>
        public UserDto UserData => Display.UserData;

        /// <summary>用户ID</summary>
        public Guid Id => UserData.Id;

        /// <summary>用户名</summary>
        public string Username => UserData.Username;

        /// <summary>显示名称</summary>
        public string DisplayName => Display.DisplayName;

        /// <summary>是否选中</summary>
        public bool IsSelected {
            get => State.IsSelected;
            set => State.IsSelected = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading {
            get => State.IsLoading;
            set {
                if (value) {
                    State.StartLoading();
                } else {
                    State.StopLoading();
                }
            }
        }

        #endregion Convenient Properties

        #region Update Methods

        /// <summary>
        /// 更新用户数据
        /// </summary>
        public void UpdateUserData(UserDto newUserData) {
            if (newUserData == null) {
                throw new ArgumentNullException(nameof(newUserData));
            }

            Display.UpdateUserData(newUserData);
            Theme.UpdateUserData(newUserData);

            // 通知相关属性变化
            RaisePropertyChanged(nameof(UserData));
            RaisePropertyChanged(nameof(Id));
            RaisePropertyChanged(nameof(Username));
            RaisePropertyChanged(nameof(DisplayName));
        }

        /// <summary>
        /// 开始编辑模式
        /// </summary>
        public void StartEditing() {
            State.StartEditing();
        }

        /// <summary>
        /// 结束编辑模式
        /// </summary>
        public void StopEditing() {
            State.StopEditing();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection() {
            State.ToggleSelection();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string errorMessage) {
            State.SetError(errorMessage);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError() {
            State.ClearError();
        }

        /// <summary>
        /// 重置UI状态
        /// </summary>
        public void ResetState() {
            State.Reset();
        }

        #endregion Update Methods

        #region Static Factory Methods

        /// <summary>
        /// 创建用户视图模型
        /// </summary>
        public static UserViewModel Create(UserDto userData) {
            return new UserViewModel(userData);
        }

        /// <summary>
        /// 从现有用户视图模型更新数据
        /// </summary>
        public static UserViewModel UpdateFrom(UserViewModel existingViewModel, UserDto newUserData) {
            existingViewModel.UpdateUserData(newUserData);
            return existingViewModel;
        }

        #endregion Static Factory Methods

        #region Equality and Comparison

        /// <summary>
        /// 判断是否为同一用户
        /// </summary>
        public bool IsSameUser(UserViewModel other) {
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// 判断是否为同一用户（通过用户数据）
        /// </summary>
        public bool IsSameUser(UserDto userData) {
            return userData != null && Id == userData.Id;
        }

        public override bool Equals(object? obj) {
            return obj is UserViewModel other && IsSameUser(other);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }

        #endregion Equality and Comparison
    }
}
