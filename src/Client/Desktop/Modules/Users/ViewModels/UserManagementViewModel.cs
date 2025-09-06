using AutoMapper;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Users.ViewModels {

    /// <summary>
    /// 用户管理视图模型 - UltraThink双层架构UI层
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：用户管理界面逻辑、命令处理、状态管理、交互控制
    /// 基于ModernManagementViewModel统一管理界面模式
    /// 集成UserModule双层服务，提供完整的用户管理用户体验
    /// 支持用户CRUD操作、状态切换、密码重置等核心管理功能
    /// 适配中医诊所用户管理流程，确保操作安全性和界面友好性
    /// </summary>
    public class UserManagementViewModel : ModernManagementViewModel<UserDto> {

        #region 私有字段和依赖注入

        private readonly IUserService _userService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #endregion 私有字段和依赖注入

        #region 额外Commands

        /// <summary>重置密码命令</summary>
        public DelegateCommand ResetPasswordCommand { get; }

        /// <summary>切换状态命令</summary>
        public DelegateCommand ToggleStatusCommand { get; }

        #endregion 额外Commands

        #region 构造函数和初始化

        /// <summary>
        /// 构造函数 - UltraThink双层架构依赖注入
        /// 初始化用户管理模块、对话服务、映射器、命令和事件订阅
        /// </summary>
        /// <param name="userService">用户模块主服务</param>
        /// <param name="dialogService">自定义对话服务</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
        public UserManagementViewModel(
            IUserService userService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService) {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 现代命令初始化 - 避免async void反模式
            ResetPasswordCommand = new DelegateCommand(async () => await ExecuteResetPasswordAsync(), () => HasSelectedItem);
            ToggleStatusCommand = new DelegateCommand(async () => await ExecuteToggleStatusAsync(), () => HasSelectedItem);
        }

        /// <summary>
        /// 兼容性构造函数 - 支持无错误处理服务的旧版本调用
        /// </summary>
        /// <param name="userService">用户模块主服务</param>
        /// <param name="dialogService">自定义对话服务</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="eventAggregator">事件聚合器</param>
        public UserManagementViewModel(
            IUserService userService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(userService, dialogService, mapper, eventAggregator, null) {
        }

        #endregion 构造函数和初始化

        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<UserDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null) {
            var userQuery = new UserPagedQueryDto {
                CurrentPage = page,
                PageSize = pageSize,
                SearchKeyword = keyword ?? string.Empty
            };
            return await _userService.GetPagedAsync(userQuery);
        }

        /// <summary>添加项</summary>
        protected override async Task OnAddAsync() {
            var parameters = new Dictionary<string, object> { ["IsEditMode"] = false };
            var result = await _dialogService.ShowDialogAsync("UserAddEditDialog", parameters);

            if (result.Result == true) {
                await _dialogService.ShowSuccessAsync("用户添加成功", "成功");
            }
        }

        /// <summary>编辑项</summary>
        protected override async Task OnEditAsync(UserDto item) {
            var parameters = new Dictionary<string, object> {
                ["IsEditMode"] = true,
                ["User"] = item
            };
            var result = await _dialogService.ShowDialogAsync("UserAddEditDialog", parameters);

            if (result.Result == true) {
                await _dialogService.ShowSuccessAsync($"用户 {item.Username} 更新成功", "成功");
            }
        }

        /// <summary>删除项（实际是禁用）</summary>
        protected override async Task OnDeleteAsync(UserDto item) {
            await ToggleUserStatusAsync(item);
        }

        /// <summary>查看详情</summary>
        protected override async Task OnViewDetailsAsync(UserDto item) {
            var result = await _userService.GetByIdAsync(item.Id);

            if (result.IsSuccess && result.Data != null) {
                var userDetail = result.Data;
                var detailInfo = $"用户详情：\n\n" +
                               $"用户名: {userDetail.Username}\n" +
                               $"角色: {userDetail.Role}\n" +
                               $"邮箱: {userDetail.Email ?? "未设置"}\n" +
                               $"电话: {userDetail.PhoneNumber ?? "未设置"}\n" +
                               $"状态: {(userDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                               $"创建时间: {userDetail.CreateTime}";

                await _dialogService.ShowInformationAsync(detailInfo, $"用户详情 - {userDetail.Username}");
            } else {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取用户详情失败", "错误");
            }
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged() {
            base.RaiseCanExecuteChanged();
            ResetPasswordCommand.RaiseCanExecuteChanged();
            ToggleStatusCommand.RaiseCanExecuteChanged();
        }

        #endregion 重写基类方法

        #region Command执行方法

        /// <summary>重置密码命令执行</summary>
        private async Task ExecuteResetPasswordAsync() {
            if (SelectedItem != null) {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"确定要重置用户 {SelectedItem.Username} 的密码吗？\n密码将重置为默认密码。",
                    "重置密码");

                if (confirm) {
                    var result = await _userService.ResetPasswordAsync(SelectedItem.Id, "ChangeMe123");
                    if (result.IsSuccess) {
                        await _dialogService.ShowInformationAsync("密码重置成功，请通知用户使用默认密码登录", "成功");
                    } else {
                        await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "密码重置失败", "错误");
                    }
                }
            }
        }

        /// <summary>切换状态命令执行</summary>
        private async Task ExecuteToggleStatusAsync() {
            if (SelectedItem != null) {
                await ToggleUserStatusAsync(SelectedItem);
            }
        }

        /// <summary>切换用户状态</summary>
        private async Task ToggleUserStatusAsync(UserDto user) {
            var isActive = user.Status == CommonStatus.Enabled;
            var action = isActive ? "禁用" : "激活";

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}用户 {user.Username} 吗？",
                $"{action}用户");

            if (confirm) {
                var result = isActive
                    ? await _userService.DisableAsync(user.Id)
                    : await _userService.EnableAsync(user.Id);

                if (result.IsSuccess) {
                    await _dialogService.ShowInformationAsync($"用户{action}成功", "成功");
                } else {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"用户{action}失败",
                        "错误");
                }
            }
        }

        #endregion Command执行方法
    }
}
