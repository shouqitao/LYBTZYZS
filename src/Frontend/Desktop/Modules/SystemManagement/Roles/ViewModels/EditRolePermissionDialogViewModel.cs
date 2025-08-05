using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Roles;
using Prism.Commands;
using Prism.Mvvm;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Roles.ViewModels
{
    /// <summary>
    /// 编辑角色权限对话框视图模型
    /// </summary>
    public class EditRolePermissionDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly RolePermissionInfo _originalRole;
        private bool _isLoading = false;

        #region 属性

        /// <summary>角色信息</summary>
        public RolePermissionInfo Role { get; }

        /// <summary>权限分组</summary>
        public ObservableCollection<PermissionGroup> PermissionGroups { get; }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>是否有更改</summary>
        public bool HasChanges => PermissionGroups.Any(g => g.Permissions.Any(p => p.HasChanged));

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ResetCommand { get; }
        public DelegateCommand SelectAllCommand { get; }
        public DelegateCommand SelectNoneCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }
        public Action<bool>? SaveCompleteCallback { get; set; }

        public EditRolePermissionDialogViewModel(RolePermissionInfo role,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _originalRole = role ?? throw new ArgumentNullException(nameof(role));
            
            // 创建副本用于编辑
            Role = CreateRoleCopy(_originalRole);
            PermissionGroups = new ObservableCollection<PermissionGroup>();

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            ResetCommand = new DelegateCommand(ExecuteReset);
            SelectAllCommand = new DelegateCommand(ExecuteSelectAll);
            SelectNoneCommand = new DelegateCommand(ExecuteSelectNone);

            // 初始化权限数据
            InitializePermissions();
        }

        private RolePermissionInfo CreateRoleCopy(RolePermissionInfo original)
        {
            return new RolePermissionInfo
            {
                Role = original.Role,
                RoleName = original.RoleName,
                Description = original.Description,
                AccessibleModules = new List<string>(original.AccessibleModules),
                IsSystemRole = original.IsSystemRole,
                IsActive = original.IsActive,
                UserCount = original.UserCount,
                CreateTime = original.CreateTime,
                UpdateTime = original.UpdateTime
            };
        }

        private void InitializePermissions()
        {
            IsLoading = true;
            try
            {
                // 定义权限分组和权限项
                var groups = new List<PermissionGroup>
                {
                    new PermissionGroup
                    {
                        GroupName = "系统管理",
                        Description = "系统级别的管理权限",
                        Permissions = new ObservableCollection<PermissionItem>
                        {
                            new("UserManagement", "用户管理", "管理系统用户，包括添加、编辑、删除用户"),
                            new("RoleManagement", "角色权限管理", "配置角色和权限设置"),
                            new("SystemSettings", "系统设置", "修改系统配置参数"),
                            new("DataBackup", "数据备份", "执行数据备份和恢复操作"),
                            new("AuditLog", "审计日志", "查看系统操作日志"),
                            new("ReportView", "报表查看", "查看各类统计报表")
                        }
                    },
                    new PermissionGroup
                    {
                        GroupName = "患者管理",
                        Description = "患者信息和档案管理权限",
                        Permissions = new ObservableCollection<PermissionItem>
                        {
                            new("PatientRegistration", "患者登记", "登记新患者信息"),
                            new("PatientEdit", "患者信息编辑", "修改患者基本信息"),
                            new("PatientHistory", "患者历史", "查看患者就诊历史"),
                            new("PatientDelete", "患者删除", "删除患者档案")
                        }
                    },
                    new PermissionGroup
                    {
                        GroupName = "诊疗管理",
                        Description = "诊断治疗相关权限",
                        Permissions = new ObservableCollection<PermissionItem>
                        {
                            new("PatientConsultation", "患者诊疗", "进行患者诊断和治疗"),
                            new("PrescriptionWrite", "处方开具", "开具中药处方"),
                            new("MedicalRecord", "病历管理", "创建和管理病历记录"),
                            new("TreatmentPlan", "治疗方案", "制定患者治疗计划"),
                            new("DiagnosisManagement", "诊断管理", "管理诊断结果")
                        }
                    },
                    new PermissionGroup
                    {
                        GroupName = "药房管理",
                        Description = "药材和处方调配权限",
                        Permissions = new ObservableCollection<PermissionItem>
                        {
                            new("HerbManagement", "药材管理", "管理中药材库存"),
                            new("PrescriptionDispense", "处方调配", "调配处方药材"),
                            new("InventoryManagement", "库存管理", "管理药材出入库"),
                            new("HerbPurchase", "药材采购", "执行药材采购操作")
                        }
                    },
                    new PermissionGroup
                    {
                        GroupName = "财务管理",
                        Description = "费用结算和财务相关权限",
                        Permissions = new ObservableCollection<PermissionItem>
                        {
                            new("Billing", "费用结算", "处理患者费用结算"),
                            new("PaymentManagement", "收费管理", "管理收费记录"),
                            new("RefundProcess", "退费处理", "处理退费申请"),
                            new("FinancialReport", "财务报表", "查看财务统计报表")
                        }
                    }
                };

                // 根据当前角色的权限设置选中状态
                foreach (var group in groups)
                {
                    foreach (var permission in group.Permissions)
                    {
                        permission.IsChecked = Role.AccessibleModules.Contains(permission.Code);
                        permission.PropertyChanged += Permission_PropertyChanged;
                    }
                }

                PermissionGroups.Clear();
                foreach (var group in groups)
                {
                    PermissionGroups.Add(group);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Permission_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PermissionItem.IsChecked))
            {
                RaisePropertyChanged(nameof(HasChanges));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private bool CanExecuteSave()
        {
            return HasChanges && !IsLoading;
        }

        private void ExecuteSave()
        {
            try
            {
                IsLoading = true;
                
                // 收集选中的权限
                var selectedPermissions = PermissionGroups
                    .SelectMany(g => g.Permissions)
                    .Where(p => p.IsChecked)
                    .Select(p => p.Code)
                    .ToList();

                // 更新角色权限
                Role.AccessibleModules = selectedPermissions;
                Role.UpdateTime = DateTime.Now;

                // TODO: 这里应该调用服务保存到后端
                // await _roleService.UpdateRolePermissionsAsync(Role.Role, selectedPermissions);

                _commonDialogService.ShowInformationAsync("角色权限保存成功！", "成功").GetAwaiter().GetResult();

                SaveCompleteCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存角色权限失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                SaveCompleteCallback?.Invoke(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancel()
        {
            if (HasChanges)
            {
                var result = _commonDialogService.ShowConfirmationAsync("有未保存的更改，确定要关闭吗？", "确认").GetAwaiter().GetResult();
                if (result != MessageBoxResult.Yes)
                    return;
            }

            CloseDialogCallback?.Invoke();
        }

        private void ExecuteReset()
        {
            var result = _commonDialogService.ShowConfirmationAsync("确定要重置所有权限设置吗？", "确认重置").GetAwaiter().GetResult();
            if (result )
            {
                InitializePermissions();
            }
        }

        private void ExecuteSelectAll()
        {
            foreach (var group in PermissionGroups)
            {
                foreach (var permission in group.Permissions)
                {
                    permission.IsChecked = true;
                }
            }
            RaisePropertyChanged(nameof(HasChanges));
        }

        private void ExecuteSelectNone()
        {
            foreach (var group in PermissionGroups)
            {
                foreach (var permission in group.Permissions)
                {
                    permission.IsChecked = false;
                }
            }
            RaisePropertyChanged(nameof(HasChanges));
        }
    }

    /// <summary>
    /// 权限分组
    /// </summary>
    public class PermissionGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ObservableCollection<PermissionItem> Permissions { get; set; } = new();
    }

    /// <summary>
    /// 权限项
    /// </summary>
    public class PermissionItem : BindableBase
    {
        private bool _isChecked;
        private bool _originalChecked;

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public bool HasChanged => _isChecked != _originalChecked;

        public PermissionItem()
        {
        }

        public PermissionItem(string code, string name, string description)
        {
            Code = code;
            Name = name;
            Description = description;
        }

        public void SetOriginalValue(bool value)
        {
            _originalChecked = value;
            _isChecked = value;
        }
    }
}