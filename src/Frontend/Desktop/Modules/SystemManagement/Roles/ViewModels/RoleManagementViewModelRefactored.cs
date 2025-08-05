using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Roles;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using Prism.Commands;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Roles.ViewModels
{
    /// <summary>
    /// 角色权限管理视图模型 - 重构版本（使用BaseManagementViewModel）
    /// </summary>
    public class RoleManagementViewModelRefactored : BaseManagementViewModel<RolePermissionInfo, IPermissionService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;

        #region 字段和属性

        /// <summary>模块名称</summary>
        protected override string ModuleName => "角色";

        /// <summary>额外的权限编辑命令</summary>
        public DelegateCommand<RolePermissionInfo> EditPermissionCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        public RoleManagementViewModelRefactored(IPermissionService permissionService,
            ICommonDialogService commonDialogService,
            IDialogService dialogService) 
            : base(permissionService)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            
            // 初始化命令以避免null引用警告
            EditPermissionCommand = new DelegateCommand<RolePermissionInfo>(_ => { });
            InitializeAdditionalCommands();
        }

        #endregion

        #region 初始化

        protected override void OnInitialize()
        {
            base.OnInitialize();
            InitializeAdditionalCommands();
        }

        private void InitializeAdditionalCommands()
        {
            EditPermissionCommand = new DelegateCommand<RolePermissionInfo>(ExecuteEditPermission, CanExecuteEditPermission);
        }

        #endregion

        #region 基类抽象方法实现

        protected override async Task<ServiceResult<PagedResult<RolePermissionInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始加载角色权限信息");

                // 获取所有枚举定义的角色
                var allRoles = Enum.GetValues<UserRole>().ToList();
                var filteredRoles = new List<RolePermissionInfo>();

                foreach (var role in allRoles)
                {
                    var roleInfo = new RolePermissionInfo
                    {
                        Role = role,
                        RoleName = Service.GetRoleDisplayName(role),
                        Description = GetRoleDescription(role),
                        AccessibleModules = Service.GetAccessibleModules(
                            new Core.Models.Users.UserInfo { Role = role }
                        ),
                        IsSystemRole = true,
                        IsActive = true,
                        UserCount = await GetUserCountByRoleAsync(role),
                        CreateTime = DateTime.Now.AddDays(-30), // 模拟创建时间
                        UpdateTime = DateTime.Now.AddDays(-1)   // 模拟更新时间
                    };

                    // 应用搜索过滤
                    if (string.IsNullOrWhiteSpace(request.SearchKeyword) || 
                        roleInfo.RoleName.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        roleInfo.Description.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        filteredRoles.Add(roleInfo);
                        System.Diagnostics.Debug.WriteLine($"添加角色: {roleInfo.RoleName}");
                    }
                }

                // 分页处理
                var totalCount = filteredRoles.Count;
                var pagedItems = filteredRoles
                    .Skip((request.CurrentPage - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var pagedResult = new PagedResult<RolePermissionInfo>
                {
                    Items = pagedItems,
                    TotalCount = totalCount,
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize
                };

                return ServiceResult<PagedResult<RolePermissionInfo>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载角色列表失败: {ex.Message}");
                return ServiceResult<PagedResult<RolePermissionInfo>>.Failure($"加载角色列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(RolePermissionInfo item)
        {
            // 系统角色不允许删除
            if (item.IsSystemRole)
            {
                return ServiceResult<bool>.Failure("系统角色不允许删除");
            }

            try
            {
                // 这里应该调用实际的删除服务
                // 目前角色管理基于枚举，不支持删除操作
                await Task.Delay(500); // 模拟异步操作
                return ServiceResult<bool>.Failure("系统角色基于枚举定义，不支持删除操作");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除角色失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(RolePermissionInfo item)
        {
            return $"{item.RoleName} ({item.Role})";
        }

        #endregion

        #region 重写的虚方法

        protected override void ExecuteAdd()
        {
            _commonDialogService.ShowInformationAsync("角色基于系统枚举定义，无法添加新角色。\n如需新增角色类型，请联系系统管理员修改代码。", "提示").GetAwaiter().GetResult();
        }

        protected override void ExecuteEdit(RolePermissionInfo item)
        {
            if (item == null) return;
            
            // 角色信息编辑重定向到权限编辑
            ExecuteEditPermission(item);
        }

        protected override void ExecuteView(RolePermissionInfo item)
        {
            if (item == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "role", item }
                };

                _dialogService.ShowDialog("ViewRoleDialog", parameters, result =>
                {
                    // 对话框关闭后的处理（如果需要）
                });
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开角色详情对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override bool CanExecuteDelete(RolePermissionInfo item)
        {
            // 系统角色不允许删除
            return item != null && !item.IsSystemRole;
        }

        #endregion

        #region 额外的权限管理功能

        private void ExecuteEditPermission(RolePermissionInfo roleInfo)
        {
            if (roleInfo == null) return;

            try
            {
                var dialog = new Views.EditRolePermissionDialog();
                dialog.Owner = Application.Current.MainWindow;

                // 设置ViewModel
                var viewModel = new EditRolePermissionDialogViewModel(roleInfo, _commonDialogService);
                // Callbacks removed - handled through dialog result

                dialog.DataContext = viewModel;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开权限编辑对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private bool CanExecuteEditPermission(RolePermissionInfo item)
        {
            return item != null;
        }

        #endregion

        #region 辅助方法

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

        private async Task<int> GetUserCountByRoleAsync(UserRole role)
        {
            try
            {
                // TODO: 从用户服务获取该角色的用户数量
                // 这里先返回模拟数据
                await Task.Delay(10); // 模拟异步操作
                
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取角色用户数量失败: {ex.Message}");
                return 0;
            }
        }

        #endregion
    }
}