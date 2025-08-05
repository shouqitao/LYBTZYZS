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
    public class ViewRoleDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

                private readonly ICommonDialogService _commonDialogService;
        private RolePermissionInfo? _role;

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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        public ViewRoleDialogViewModel(ICommonDialogService commonDialogService)
        {
            Title = "角色详情";
            _commonDialogService = commonDialogService;

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("roleId"))
            {
                var id = parameters.GetValue<Guid>("roleId");
                _ = LoadRoleAsync(id);
            }

        }
        
        private async System.Threading.Tasks.Task LoadRoleAsync(Guid roleId)
        {
            try
            {
                IsLoading = true;
                // TODO: Implement loading logic
                await System.Threading.Tasks.Task.Delay(100);
            }
            catch (Exception)
            {
                // Handle error
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClose()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private async void ExecutePrint()
        {
            // TODO: 实现打印功能
            await _commonDialogService.ShowInformationAsync("角色信息打印功能开发中...", "提示");
        }
        // 临时占位方法 - 等待IDialogAware问题解决
        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            // TODO: 实现对话框关闭逻辑
        }



        /* #region IDialogAware Implementation

        event Action<IDialogResult> IDialogAware.RequestClose
        {
            add { _requestClose += value; }
            remove { _requestClose -= value; }
        }
        
        private Action<IDialogResult>? _requestClose;

        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            _requestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        #endregion */
        }
}