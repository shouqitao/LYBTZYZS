using System;
using System.Windows;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    public partial class SimpleAddRegistrationDialog : Window
    {
        private readonly ICommonDialogService _commonDialogService;

        public SimpleAddRegistrationDialog(ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            InitializeComponent();
            dpDate.SelectedDate = DateTime.Today;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 简单验证
            if (string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                _commonDialogService.ShowWarningAsync("请输入患者姓名", "提示").GetAwaiter().GetResult();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtPatientPhone.Text))
            {
                _commonDialogService.ShowWarningAsync("请输入患者电话", "提示").GetAwaiter().GetResult();
                return;
            }
            
            if (cboDepartment.SelectedItem == null)
            {
                _commonDialogService.ShowWarningAsync("请选择科室", "提示").GetAwaiter().GetResult();
                return;
            }
            
            if (cboType.SelectedItem == null)
            {
                _commonDialogService.ShowWarningAsync("请选择挂号类型", "提示").GetAwaiter().GetResult();
                return;
            }
            
            if (!dpDate.SelectedDate.HasValue)
            {
                _commonDialogService.ShowWarningAsync("请选择就诊日期", "提示").GetAwaiter().GetResult();
                return;
            }
            
            // TODO: 这里应该调用API保存挂号信息
            _commonDialogService.ShowInformationAsync("挂号信息已保存（功能开发中）", "成功").GetAwaiter().GetResult();
            
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}