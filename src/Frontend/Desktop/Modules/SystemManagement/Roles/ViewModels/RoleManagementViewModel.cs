using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Roles;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Roles.ViewModels
{
    /// <summary>
    /// 角色权限管理视图模型
    /// </summary>
    public class RoleManagementViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IPermissionService _permissionService;
        private string _searchKeyword = string.Empty;
        private RolePermissionInfo? _selectedRole;
        private bool _isLoading = false;

        public ObservableCollection<RolePermissionInfo> Roles { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<RolePermissionInfo> ViewRoleCommand { get; }
        public DelegateCommand<RolePermissionInfo> EditPermissionCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的角色</summary>
        public RolePermissionInfo? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {Roles.Count} 个角色";

        public RoleManagementViewModel(IPermissionService permissionService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _permissionService = permissionService;
            Roles = new ObservableCollection<RolePermissionInfo>();

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            ViewRoleCommand = new DelegateCommand<RolePermissionInfo>(ExecuteViewRole);
            EditPermissionCommand = new DelegateCommand<RolePermissionInfo>(ExecuteEditPermission);

            // 加载初始数据
            LoadRoles();
        }

        private void LoadRoles()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine("开始加载角色权限信息");

                Roles.Clear();

                // 获取所有枚举定义的角色
                var allRoles = Enum.GetValues<UserRole>();

                foreach (var role in allRoles)
                {
                    var roleInfo = new RolePermissionInfo
                    {
                        Role = role,
                        RoleName = _permissionService.GetRoleDisplayName(role),
                        Description = GetRoleDescription(role),
                        AccessibleModules = _permissionService.GetAccessibleModules(
                            new Core.Models.Users.UserInfo { Role = role }
                        ),
                        IsSystemRole = true,
                        IsActive = true,
                        UserCount = GetUserCountByRole(role) // 需要从用户服务获取
                    };

                    // 应用搜索过滤
                    if (string.IsNullOrWhiteSpace(SearchKeyword) || 
                        roleInfo.RoleName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        roleInfo.Description.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        Roles.Add(roleInfo);
                        System.Diagnostics.Debug.WriteLine($"添加角色: {roleInfo.RoleName}");
                    }
                }

                RaisePropertyChanged(nameof(StatusText));
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载角色列表失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetRoleDescription(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "系统管理员，拥有最高权限，负责系统配置和用户管理",
                UserRole.DiagnosingDoctor => "主治医生，负责患者诊断治疗和处方开具",
                UserRole.RegistrationStaff => "挂号人员，负责患者挂号登记和基础信息管理",
                UserRole.CashierStaff => "收费人员，负责费用结算和财务相关操作",
                UserRole.PharmacyStaff => "药剂师，负责处方调配和药材管理",
                UserRole.PhysiotherapyStaff => "理疗师，负责物理治疗和康复指导",
                _ => "未知角色"
            };
        }

        private int GetUserCountByRole(UserRole role)
        {
            // TODO: 从用户服务获取该角色的用户数量
            // 这里先返回模拟数据
            return role switch
            {
                UserRole.Admin => 2,
                UserRole.DiagnosingDoctor => 8,
                UserRole.RegistrationStaff => 3,
                UserRole.CashierStaff => 2,
                UserRole.PharmacyStaff => 4,
                UserRole.PhysiotherapyStaff => 3,
                _ => 0
            };
        }

        private void ExecuteSearch()
        {
            System.Diagnostics.Debug.WriteLine($"执行搜索，关键词: '{SearchKeyword}'");
            LoadRoles();
        }

        private void ExecuteRefresh()
        {
            LoadRoles();
        }

        private void ExecuteViewRole(RolePermissionInfo roleInfo)
        {
            if (roleInfo == null) return;

            try
            {
                var dialog = new Views.ViewRoleDialog();
                dialog.Owner = Application.Current.MainWindow;

                // 设置ViewModel
                var viewModel = new ViewModels.ViewRoleDialogViewModel(_commonDialogService);

                dialog.DataContext = viewModel;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开角色详情对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteEditPermission(RolePermissionInfo roleInfo)
        {
            if (roleInfo == null) return;

            try
            {
                var dialog = new Views.EditRolePermissionDialog();
                dialog.Owner = Application.Current.MainWindow;

                // 设置ViewModel
                var viewModel = new ViewModels.EditRolePermissionDialogViewModel(roleInfo, _commonDialogService);
                // Callbacks removed - handled through dialog result

                dialog.DataContext = viewModel;
                if (dialog.ShowDialog() == true)
                {
                    LoadRoles(); // 刷新列表
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开权限编辑对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }
    }
}