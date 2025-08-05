using System;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Roles;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Roles.ViewModels
{
    /// <summary>
    /// 查看角色详情对话框视图模型
    /// </summary>
    public class ViewRoleDialogViewModel : BindableBase
    {
        private readonly RolePermissionInfo _role;

        #region 属性

        /// <summary>角色信息</summary>
        public RolePermissionInfo Role => _role;

        /// <summary>权限模块列表文本</summary>
        public string PermissionModulesText => _role.AccessibleModules.Any() 
            ? string.Join("、", _role.AccessibleModules)
            : "无权限模块";

        /// <summary>角色类型描述</summary>
        public string RoleTypeDescription => _role.IsSystemRole ? "系统内置角色" : "自定义角色";

        /// <summary>创建时间描述</summary>
        public string CreateTimeDescription => _role.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>更新时间描述</summary>
        public string UpdateTimeDescription => _role.UpdateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "从未更新";

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }

        public ViewRoleDialogViewModel(RolePermissionInfo role)
        {
            _role = role ?? throw new ArgumentNullException(nameof(role));

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
        }

        private void ExecuteClose()
        {
            CloseDialogCallback?.Invoke();
        }

        private void ExecutePrint()
        {
            // TODO: 实现打印功能
            System.Windows.MessageBox.Show("角色信息打印功能开发中...", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}