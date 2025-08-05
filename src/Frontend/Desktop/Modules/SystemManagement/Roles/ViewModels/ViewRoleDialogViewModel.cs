using System;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Roles;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Modules.SystemManagement.Roles.ViewModels
{
    /// <summary>
    /// 查看角色详情对话框视图模型
    /// </summary>
    public class ViewRoleDialogViewModel : BindableBase, IDialogAware
    {
        private readonly ICommonDialogService _commonDialogService;
        private RolePermissionInfo? _role;

        #region IDialogAware

        public string Title => "角色详情";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("role"))
            {
                Role = parameters.GetValue<RolePermissionInfo>("role");
            }
        }

        #endregion

        #region 属性

        /// <summary>角色信息</summary>
        public RolePermissionInfo? Role
        {
            get => _role;
            set
            {
                SetProperty(ref _role, value);
                RaisePropertyChanged(nameof(PermissionModulesText));
                RaisePropertyChanged(nameof(RoleTypeDescription));
                RaisePropertyChanged(nameof(CreateTimeDescription));
                RaisePropertyChanged(nameof(UpdateTimeDescription));
            }
        }

        /// <summary>权限模块列表文本</summary>
        public string PermissionModulesText => _role?.AccessibleModules.Any() == true
            ? string.Join("、", _role.AccessibleModules)
            : "无权限模块";

        /// <summary>角色类型描述</summary>
        public string RoleTypeDescription => _role?.IsSystemRole == true ? "系统内置角色" : "自定义角色";

        /// <summary>创建时间描述</summary>
        public string CreateTimeDescription => _role?.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";

        /// <summary>更新时间描述</summary>
        public string UpdateTimeDescription => _role?.UpdateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "从未更新";

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }

        #endregion

        public ViewRoleDialogViewModel(ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        private async void ExecutePrint()
        {
            // TODO: 实现打印功能
            await _commonDialogService.ShowInformationAsync("角色信息打印功能开发中...", "提示");
        }
    }
}