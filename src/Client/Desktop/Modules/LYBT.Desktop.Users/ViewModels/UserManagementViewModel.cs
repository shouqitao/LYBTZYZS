using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// �û�������ͼģ�� - Phase 1�ܹ��ع��汾
    /// �����µ�ListPageViewModelʵ���������û���������
    /// </summary>
    public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
    {
        #region ��������

        private readonly IUserService _userService;

        #endregion

        #region ɸѡ����

        private UserRole? _selectedRole;
        private CommonStatus? _selectedStatus;
        private bool _showInactiveUsers;

        /// <summary>
        /// ѡ�еĽ�ɫɸѡ
        /// </summary>
        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// ѡ�е�״̬ɸѡ
        /// </summary>
        public CommonStatus? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// �Ƿ���ʾ�ѽ����û�
        /// </summary>
        public bool ShowInactiveUsers
        {
            get => _showInactiveUsers;
            set
            {
                if (SetProperty(ref _showInactiveUsers, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// ��ɫѡ��
        /// </summary>
        public IEnumerable<UserRole> RoleOptions { get; }

        /// <summary>
        /// ״̬ѡ��
        /// </summary>
        public IEnumerable<CommonStatus> StatusOptions { get; }

        #endregion

        #region �û��ض�����

        /// <summary>
        /// �༭�û�����
        /// </summary>
        /// <summary>
        /// 编辑用户命令
        /// </summary>
        public DelegateCommand<UserDto> EditCommand { get; private set; } = null!;

        /// <summary>
        /// ������������
        /// </summary>
        public DelegateCommand<UserDto> ResetPasswordCommand { get; private set; } = null!;

        /// <summary>
        /// ����/�����û�����
        /// </summary>
        public DelegateCommand<UserDto> ToggleUserStatusCommand { get; private set; } = null!;

        /// <summary>
        /// �鿴��������
        /// </summary>
        public DelegateCommand<UserDto> ViewDetailsCommand { get; private set; } = null!;

        /// <summary>
        /// ���ɸѡ����
        /// </summary>
        public DelegateCommand ClearFiltersCommand { get; private set; } = null!;

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; } = null!;

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region ���캯��

        public UserManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // ��ʼ��ѡ��
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // ��ʼ��ҳ������
            PageTitle = "�û�����";
            PageSize = 20;

            // ��ʼ���û��ض�����
            InitializeUserCommands();

            Logger.LogDebug("�û�����ViewModel�ѳ�ʼ��");
        }

        #endregion

        #region �����ʼ��

        private void InitializeUserCommands()
        {
            EditCommand = new DelegateCommand<UserDto>(ExecuteEditUser, CanExecuteEditUser);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);
            ResetPasswordCommand = new DelegateCommand<UserDto>(async user => await ExecuteResetPasswordAsync(user), CanExecuteResetPassword);
            ToggleUserStatusCommand = new DelegateCommand<UserDto>(async user => await ExecuteToggleUserStatusAsync(user), CanExecuteToggleUserStatus);
            ViewDetailsCommand = new DelegateCommand<UserDto>(ExecuteViewDetails, user => user != null);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
        }

        #endregion

        #region 暴露基类命令

        /// <summary>
        /// 搜索命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand SearchCommand => base.SearchCommand;

        /// <summary>
        /// 刷新命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand RefreshCommand => base.RefreshCommand;

        /// <summary>
        /// 添加命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand AddCommand => base.AddCommand;

        /// <summary>
        /// 删除命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand<UserDto> DeleteCommand => base.DeleteCommand;

        /// <summary>
        /// 上一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;

        /// <summary>
        /// 下一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand NextPageCommand => base.NextPageCommand;

        #endregion

        #region 导航处理

        /// <summary>
        /// 页面导航完成时触发
        /// </summary>
        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);
            await LoadPageAsync();
        }

        #endregion

        #region ���ݼ���

        /// <summary>
        /// ��ȡ������
        /// </summary>
        protected override async Task<IEnumerable<UserDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("�����û�����: ��{Page}ҳ, ÿҳ{PageSize}��, �ؼ���: {SearchText}", page, pageSize, searchText);

            try
            {
                // ������ѯ����������򻯴�����ʵ�ʿ�����Ҫ�����ӵĲ�ѯ��������
                var result = await _userService.GetPagedAsync(page, pageSize, searchText);

                if (result.IsSuccess && result.Data != null)
                {
                    var pagedData = result.Data;

                    // �����ɸѡ�������ڿͻ��˽�һ�����ˣ�ʵ����Ŀ��Ӧ���ڷ���˴�����
                    var filteredItems = pagedData.Items.AsEnumerable();

                    if (SelectedRole.HasValue)
                    {
                        filteredItems = filteredItems.Where(u => u.Role == SelectedRole.Value);
                    }

                    if (SelectedStatus.HasValue)
                    {
                        filteredItems = filteredItems.Where(u => u.Status == SelectedStatus.Value);
                    }

                    if (!ShowInactiveUsers)
                    {
                        filteredItems = filteredItems.Where(u => u.Status == CommonStatus.Enabled);
                    }

                    // ��������
                    TotalCount = pagedData.TotalCount;
                    return filteredItems;
                }
                else
                {
                    Logger.LogWarning("�����û�����ʧ��: {ErrorMessage}", result.ErrorMessage);
                    TotalCount = 0;
                    return new List<UserDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�����û�����ʱ�����쳣");
                var contextMessage = $"加载用户列表 - 模块:{nameof(UserManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);

                TotalCount = 0;
                return new List<UserDto>();
            }
        }

        #endregion

        #region �û�����ʵ��

        /// <summary>
        /// �������û�
        /// </summary>
        protected override Task OnExecuteAddAsync()
        {
            Logger.LogDebug("ִ���������û�");

            // �������û�����ҳ��
            NavigateTo("ContentRegion", "UserCreateView", new Prism.Regions.NavigationParameters
            {
                { "title", "�����û�" }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// ɾ���û�
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("ɾ���û�: {UserId} - {UserName}", user.Id, user.UserName);

            var result = await _userService.DeleteAsync(user.Id);
            if (!result.IsSuccess)
            {
                Logger.LogWarning("ɾ���û�ʧ��: {ErrorMessage}", result.ErrorMessage);
                throw new InvalidOperationException($"ɾ���û�ʧ��: {result.ErrorMessage}");
            }

            Logger.LogInformation("�ɹ�ɾ���û�: {UserName}", user.UserName);
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<UserDto> users)
        {
            Logger.LogDebug("批量删除{Count}个用户", users.Count);

            var failedUsers = new List<string>();

            foreach (var user in users)
            {
                try
                {
                    var result = await _userService.DeleteAsync(user.Id);
                    if (!result.IsSuccess)
                    {
                        failedUsers.Add($"{user.UserName}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "删除用户失败: {UserName}", user.UserName);
                    failedUsers.Add($"{user.UserName}: {ex.Message}");
                }
            }

            if (failedUsers.Count > 0)
            {
                var errorMessage = $"以下用户删除失败：{string.Join("; ", failedUsers)}";
                Logger.LogWarning("批量删除部分失败: {FailedCount}/{TotalCount}", failedUsers.Count, users.Count);
                throw new InvalidOperationException(errorMessage);
            }

            Logger.LogInformation("成功批量删除{Count}个用户", users.Count);
        }

        #endregion

        #region �û��ض�����ʵ��

        /// <summary>
        /// �༭�û�
        /// </summary>
        private void ExecuteEditUser(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("�༭�û�: {UserId} - {UserName}", user.Id, user.UserName);

            NavigateTo("ContentRegion", "UserEditView", new Prism.Regions.NavigationParameters
            {
                { "userId", user.Id },
                { "title", $"�༭�û� - {user.RealName}" }
            });
        }

        /// <summary>
        /// �Ƿ���Ա༭�û�
        /// </summary>
        private bool CanExecuteEditUser(UserDto user)
        {
            return user != null && !IsLoading;
        }

        /// <summary>
        /// ��������
        /// </summary>
        private async Task ExecuteResetPasswordAsync(UserDto user)
        {
            if (user == null) return;

            await ExecuteSafelyAsync(() =>
            {
                Logger.LogDebug("�����û�����: {UserId} - {UserName}", user.Id, user.UserName);

                // ����Ӧ�õ����������÷��񣬻��ߴ���������Ի���
                // ��ʱ��¼��־
                Logger.LogInformation("�û� {UserName} �����������������ύ", user.UserName);

                // ʵ��ʵ�ֿ�����Ҫ��
                // 1. ����������Ի���
                // 2. ������������API
                // 3. ��������֪ͨ

                return Task.CompletedTask;
            }, "��������");
        }

        /// <summary>
        /// �Ƿ������������
        /// </summary>
        private bool CanExecuteResetPassword(UserDto user)
        {
            return user != null && !IsLoading && user.Status == CommonStatus.Enabled;
        }

        /// <summary>
        /// �л��û�״̬
        /// </summary>
        private async Task ExecuteToggleUserStatusAsync(UserDto user)
        {
            if (user == null) return;

            await ExecuteSafelyAsync(async () =>
            {
                var newStatus = user.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var action = newStatus == CommonStatus.Enabled ? "����" : "����";

                Logger.LogDebug("{Action}�û�: {UserId} - {UserName}", action, user.Id, user.UserName);

                var updateDto = new UserUpdateDto
                {
                    Id = user.Id,
                    Status = newStatus
                };

                var result = await _userService.UpdateAsync(user.Id, updateDto);
                if (result.IsSuccess)
                {
                    Logger.LogInformation("�ɹ�{Action}�û�: {UserName}", action, user.UserName);
                    await LoadPageAsync(); // ˢ������
                }
                else
                {
                    Logger.LogWarning("{Action}�û�ʧ��: {ErrorMessage}", action, result.ErrorMessage);
                    throw new InvalidOperationException($"{action}�û�ʧ��: {result.ErrorMessage}");
                }

            }, user.Status == CommonStatus.Enabled ? "�����û�" : "�����û�");
        }

        /// <summary>
        /// �Ƿ�����л��û�״̬
        /// </summary>
        private bool CanExecuteToggleUserStatus(UserDto user)
        {
            return user != null && !IsLoading;
        }

        /// <summary>
        /// �鿴����
        /// </summary>
        private void ExecuteViewDetails(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("�鿴�û�����: {UserId} - {UserName}", user.Id, user.UserName);

            NavigateTo("ContentRegion", "UserDetailsView", new Prism.Regions.NavigationParameters
            {
                { "userId", user.Id },
                { "title", $"�û����� - {user.RealName}" }
            });
        }

        /// <summary>
        /// ���ɸѡ
        /// </summary>
        private void ExecuteClearFilters()
        {
            SelectedRole = null;
            SelectedStatus = null;
            ShowInactiveUsers = false;
            SearchText = string.Empty;
        }

        /// <summary>
        /// �Ƿ��лɸѡ
        /// </summary>
        private bool HasActiveFilters =>
            SelectedRole.HasValue ||
            SelectedStatus.HasValue ||
            ShowInactiveUsers ||
            !string.IsNullOrEmpty(SearchText);

        #endregion

        #region ����ˢ��

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            EditCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
