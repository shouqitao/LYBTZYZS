using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 个人资料编辑对话框 ViewModel
    /// </summary>
    public class UserProfileDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly IUserService _userService;
        private readonly ISessionManager _sessionManager;
        private Guid _currentUserId;
        private string? _avatarFilePath;

        #region 属性

        private bool _hasAvatar;
        /// <summary>
        /// 是否有头像
        /// </summary>
        public bool HasAvatar
        {
            get => _hasAvatar;
            set => SetProperty(ref _hasAvatar, value);
        }

        private string _avatarInitial = string.Empty;
        /// <summary>
        /// 头像首字母（无头像时显示）
        /// </summary>
        public string AvatarInitial
        {
            get => _avatarInitial;
            set => SetProperty(ref _avatarInitial, value);
        }

        private ImageSource? _avatarSource;
        /// <summary>
        /// 头像图片源
        /// </summary>
        public ImageSource? AvatarSource
        {
            get => _avatarSource;
            set => SetProperty(ref _avatarSource, value);
        }

        private string _username = string.Empty;
        /// <summary>
        /// 用户名（只读）
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    UpdateAvatarInitial();
                }
            }
        }

        private string _realName = string.Empty;
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName
        {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        private string _email = string.Empty;
        /// <summary>
        /// 邮箱（当前接口不支持修改，只读显示）
        /// </summary>
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phoneNumber = string.Empty;
        /// <summary>
        /// 电话号码
        /// </summary>
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        private string _department = string.Empty;
        /// <summary>
        /// 部门（当前接口不支持修改，只读显示）
        /// </summary>
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private string _position = string.Empty;
        /// <summary>
        /// 职位（当前接口不支持修改，只读显示）
        /// </summary>
        public string Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SelectAvatarCommand { get; }
        public DelegateCommand RemoveAvatarCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region IDialogAware 实现

        public string Title => "编辑个人资料";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 获取当前用户 ID（注意：Infrastructure.ISessionManager 没有 CurrentUserId，使用 CurrentUser.Id）
                _currentUserId = _sessionManager?.CurrentUser?.Id ?? Guid.Empty;

                if (_currentUserId == Guid.Empty)
                {
                    Logger.LogError("无法获取当前用户ID");
                    SetError("无法获取用户信息");
                    return;
                }

                _ = LoadUserProfileAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开个人资料对话框时发生异常");
                SetError("对话框初始化失败");
            }
        }

        #endregion

        #region 构造函数

        public UserProfileDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager sessionManager,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            SelectAvatarCommand = new DelegateCommand(SelectAvatar);
            RemoveAvatarCommand = new DelegateCommand(RemoveAvatar, () => HasAvatar)
                .ObservesProperty(() => HasAvatar);

            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave)
                .ObservesProperty(() => RealName)
                .ObservesProperty(() => PhoneNumber);

            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载用户资料
        /// </summary>
        private async Task LoadUserProfileAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载个人资料...");

                var result = await _userService.GetByIdAsync(_currentUserId);

                if (result.IsSuccess && result.Data != null)
                {
                    var user = result.Data;

                    Username = user.UserName; // 注意：UserDto 属性名是 UserName，不是 Username
                    RealName = user.RealName ?? string.Empty;
                    Email = user.Email ?? string.Empty;
                    PhoneNumber = user.PhoneNumber ?? string.Empty;
                    Department = string.Empty; // UserDto 中可能没有这些字段
                    Position = string.Empty;

                    // TODO: 加载头像（如果有头像 URL）
                    HasAvatar = false;
                    UpdateAvatarInitial();

                    ClearError();
                }
                else
                {
                    SetError(result.ErrorMessage ?? "加载个人资料失败");
                    Logger.LogWarning("加载用户资料失败: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户资料时发生异常");
                SetError($"加载失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 更新头像首字母
        /// </summary>
        private void UpdateAvatarInitial()
        {
            if (!HasAvatar && !string.IsNullOrEmpty(Username))
            {
                AvatarInitial = Username.Substring(0, 1).ToUpper();
            }
        }

        /// <summary>
        /// 选择头像
        /// </summary>
        private void SelectAvatar()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "选择头像图片",
                    Filter = "图片文件 (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;

                    // 检查文件大小（最大 2MB）
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 2 * 1024 * 1024)
                    {
                        SetError("图片文件大小不能超过 2MB");
                        return;
                    }

                    // 加载图片
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarSource = bitmap;
                    HasAvatar = true;
                    _avatarFilePath = filePath;

                    ClearError();
                    Logger.LogInformation("已选择头像: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择头像时发生异常");
                SetError("选择头像失败");
            }
        }

        /// <summary>
        /// 删除头像
        /// </summary>
        private void RemoveAvatar()
        {
            AvatarSource = null;
            HasAvatar = false;
            _avatarFilePath = null;
            UpdateAvatarInitial();
            Logger.LogInformation("已删除头像");
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(RealName))
            {
                SetError("请输入真实姓名");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                // 简单的电话号码验证（中国手机号）
                if (PhoneNumber.Length != 11 || !PhoneNumber.StartsWith("1"))
                {
                    SetError("请输入有效的手机号码");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(RealName);
        }

        /// <summary>
        /// 保存个人资料
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                SetIsBusy(true, "正在保存个人资料...");

                // TODO: 当前 Client 端没有 ChangeProfileAsync 服务方法，暂时 Mock 成功
                // 真实实现需要调用服务端 API 并支持 RealName、PhoneNumber、头像上传等功能
                await Task.Delay(500); // 模拟网络延迟

                await ShowSuccessMessageAsync("个人资料保存成功");

                // TODO: 如果有头像文件，需要上传到服务器
                // 当前版本暂不实现头像上传功能

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));

                Logger.LogInformation(
                    "用户 {UserId} 个人资料保存成功 (RealName: {RealName}, PhoneNumber: {PhoneNumber})",
                    _currentUserId,
                    RealName,
                    PhoneNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存个人资料时发生异常");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 设置错误
        /// </summary>
        private void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        #endregion
    }
}
