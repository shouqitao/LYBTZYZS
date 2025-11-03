using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components; // Issue #1785: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户创建视图模型 - Phase 1重构简化版本
    /// 基于新的PageViewModel实现用户创建功能
    /// </summary>
    public class UserCreateViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        // Issue #1785: 使用CommandHandler替代直接Repository访问
        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region 用户输入属性

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string? _phoneNumber;
        private string? _email;
        private UserRole _selectedRole = UserRole.Doctor;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        public string UserName
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        public string RealName
        {
            get => _realName;
            set
            {
                if (SetProperty(ref _realName, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string? Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 选中的角色
        /// </summary>
        public UserRole SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        /// <summary>
        /// 用户状态
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 角色选项
        /// </summary>
        public UserRole[] RoleOptions { get; }

        /// <summary>
        /// 状态选项
        /// </summary>
        public CommonStatus[] StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 创建用户命令
        /// </summary>
        public DelegateCommand CreateUserCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 重置表单命令
        /// </summary>
        public DelegateCommand ResetFormCommand { get; }

        #endregion

        #region 构造函数

        public UserCreateViewModel(
            UserCommandHandler commandHandler, // Issue #1785: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1785: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            CreateUserCommand = new DelegateCommand(async () => await CreateUserAsync(), CanCreateUser);
            CancelCommand = new DelegateCommand(Cancel);
            ResetFormCommand = new DelegateCommand(ResetForm);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => CreateUserCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 创建用户
        /// Issue #1262: 优化成功后的流程，先清理状态再导航
        /// </summary>
        private async Task CreateUserAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在创建用户...";

                var createDto = BuildUserInputDto();
                var result = await _commandHandler.CreateAsync(createDto);

                if (result.success && result.user != null)
                    HandleCreateSuccess(result.user);
                else
                    HandleCreateFailure(result.errorMessage);
            }
            catch (LYBT.Shared.Models.Exceptions.ApiException apiEx)
            {
                HandleApiException(apiEx);
            }
            catch (Exception ex)
            {
                HandleSystemException(ex);
            }
        }

        /// <summary>
        /// 构建用户输入DTO
        /// Issue #1794: 从CreateUserAsync提取
        /// </summary>
        private UserInputDto BuildUserInputDto()
        {
            return new UserInputDto
            {
                UserName = UserName.Trim(),
                RealName = RealName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Role = SelectedRole,
                Status = Status
            };
        }

        /// <summary>
        /// 处理创建用户成功
        /// Issue #1794: 从CreateUserAsync提取
        /// </summary>
        private void HandleCreateSuccess(UserDto user)
        {
            Logger.LogInformation("用户创建成功: {UserName} (ID: {UserId})", user.UserName, user.Id);

            IsBusy = false;
            StatusMessage = string.Empty;

            System.Windows.MessageBox.Show(
                $"用户 '{user.UserName}' 创建成功！\n真实姓名：{user.RealName}",
                "创建成功",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            NavigateToUserManagement();
        }

        /// <summary>
        /// 处理创建用户失败
        /// Issue #1794: 从CreateUserAsync提取
        /// </summary>
        private void HandleCreateFailure(string? errorMessage)
        {
            IsBusy = false;
            ErrorMessage = errorMessage ?? "创建用户失败";
            Logger.LogError("创建用户失败：{ErrorMessage}", errorMessage);
            System.Windows.MessageBox.Show(ErrorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        /// <summary>
        /// 处理API业务异常
        /// Issue #1794: 从CreateUserAsync提取
        /// </summary>
        private void HandleApiException(LYBT.Shared.Models.Exceptions.ApiException apiEx)
        {
            IsBusy = false;
            var errorMessage = ExtractFriendlyErrorMessage(apiEx);
            Logger.LogWarning(apiEx, "创建用户业务失败: {ErrorMessage}", errorMessage);

            ErrorMessage = errorMessage;
            System.Windows.MessageBox.Show(errorMessage, "提示",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }

        /// <summary>
        /// 处理系统异常
        /// Issue #1794: 从CreateUserAsync提取
        /// </summary>
        private void HandleSystemException(Exception ex)
        {
            IsBusy = false;
            Logger.LogError(ex, "创建用户时发生系统异常");
            HandleError(ex, "创建用户");
        }

        /// <summary>
        /// 检查是否可以创建用户
        /// </summary>
        /// &lt;summary&gt;
        /// 检查是否可以创建用户
        /// Issue #1261: 移除密码验证，新用户使用系统默认密码
        /// &lt;/summary&gt;
        private bool CanCreateUser()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            NavigateToUserManagement();
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        /// &lt;summary&gt;
        /// 重置表单
        /// Issue #1261: 移除密码字段重置
        /// &lt;/summary&gt;
        private void ResetForm()
        {
            UserName = string.Empty;
            RealName = string.Empty;
            PhoneNumber = null;
            Email = null;
            SelectedRole = UserRole.Doctor;
            Status = CommonStatus.Enabled;

            // 清除验证错误
            ClearValidationErrors();
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证确认密码
        /// </summary>
        /// &lt;summary&gt;
        /// 验证属性
        /// Issue #1261: 移除密码验证逻辑
        /// &lt;/summary&gt;
        protected override void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            // 基础验证通过DataAnnotations自动处理

            // 特殊验证：手机号码格式
            if (propertyName == nameof(PhoneNumber) && !string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ClearValidationErrors(nameof(PhoneNumber));
                if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber, @"^1[3-9]\d{9}$"))
                {
                    AddValidationError(nameof(PhoneNumber), "请输入有效的手机号码");
                }
            }

            // 特殊验证：邮箱格式
            if (propertyName == nameof(Email) && !string.IsNullOrWhiteSpace(Email))
            {
                ClearValidationErrors(nameof(Email));
                if (!System.Text.RegularExpressions.Regex.IsMatch(Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    AddValidationError(nameof(Email), "请输入有效的邮箱地址");
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 导航到用户管理页面
        /// Issue #1262: 添加详细日志以追踪导航问题
        /// </summary>
        private void NavigateToUserManagement()
        {
            Logger.LogInformation("开始导航到用户管理页面 (Region: AdminContentRegion, View: UserManagementView)");
            try
            {
                NavigateTo("AdminContentRegion", "UserManagementView");
                Logger.LogInformation("导航请求已发送");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到用户管理页面失败");
                throw;
            }
        }

        /// <summary>
        /// 从 ApiException 提取友好的错误消息
        /// Issue #1262: 优先显示业务错误消息，健壮处理各种响应格式
        /// </summary>
        private string ExtractFriendlyErrorMessage(LYBT.Shared.Models.Exceptions.ApiException apiEx)
        {
            // 优先使用 ResponseContent（可能包含 JSON 格式的业务错误消息）
            if (!string.IsNullOrWhiteSpace(apiEx.ResponseContent))
            {
                try
                {
                    // 尝试解析 JSON 响应
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(apiEx.ResponseContent);

                    // 尝试提取 message 字段
                    if (jsonDoc.RootElement.TryGetProperty("message", out var messageProperty))
                    {
                        var message = messageProperty.GetString();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            return message;
                        }
                    }

                    // 尝试提取 title 字段（ProblemDetails 格式）
                    if (jsonDoc.RootElement.TryGetProperty("title", out var titleProperty))
                    {
                        var title = titleProperty.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            return title;
                        }
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // JSON 解析失败，忽略并回退到其他方式
                    Logger.LogDebug("无法解析 API 错误响应为 JSON，使用异常消息");
                }
                catch (Exception ex)
                {
                    // 其他异常也记录但不影响流程
                    Logger.LogWarning(ex, "提取错误消息时发生异常");
                }
            }

            // 回退到异常消息
            return apiEx.Message ?? "API 调用失败";
        }

        #endregion
    }
}
