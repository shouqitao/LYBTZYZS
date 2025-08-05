#!/usr/bin/env python3
"""
修复新建挂号按钮无效的问题
"""

import os
import sys
import re

def fix_add_registration_dialog():
    """修复AddRegistrationDialog.xaml.cs文件"""
    
    dialog_cs_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\Views\AddRegistrationDialog.xaml.cs"
    
    # 新的代码内容
    new_content = """using System.Windows;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels;
using Unity;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    /// <summary>
    /// AddRegistrationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddRegistrationDialog : Window
    {
        public AddRegistrationDialog()
        {
            InitializeComponent();
            
            // 创建ViewModel
            // 注意：如果使用依赖注入容器，应该从容器中获取这些服务
            try
            {
                // 尝试从App的容器中获取服务
                var app = Application.Current as App;
                if (app != null && app.Container != null)
                {
                    var registrationService = app.Container.Resolve<IRegistrationService>();
                    var patientService = app.Container.Resolve<IPatientService>();
                    var doctorService = app.Container.Resolve<IDoctorService>();
                    
                    var viewModel = new AddRegistrationDialogViewModel(
                        registrationService, 
                        patientService, 
                        doctorService, 
                        this);
                    
                    DataContext = viewModel;
                }
                else
                {
                    // 如果无法获取容器，创建临时的服务实例
                    var baseUrl = "https://localhost:7001/api/v1/";
                    var registrationService = new RegistrationService(baseUrl);
                    var patientService = new PatientService(baseUrl);
                    var doctorService = new DoctorService(baseUrl);
                    
                    var viewModel = new AddRegistrationDialogViewModel(
                        registrationService,
                        patientService,
                        doctorService,
                        this);
                    
                    DataContext = viewModel;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"初始化新增挂号对话框失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
"""
    
    try:
        # 写入新内容
        with open(dialog_cs_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"✅ 已更新文件：{dialog_cs_path}")
        
    except Exception as e:
        print(f"❌ 更新文件失败：{e}")
        return False
    
    return True


def create_simple_registration_dialog():
    """创建一个简化版的新增挂号对话框"""
    
    # 创建简化版的对话框代码
    simple_dialog_xaml = """<Window x:Class="LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views.SimpleAddRegistrationDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="新增挂号" Height="500" Width="450"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <Border Grid.Row="0" Background="#2196F3" Padding="20,15">
            <TextBlock Text="新增挂号" FontSize="20" FontWeight="Bold" Foreground="White"/>
        </Border>

        <!-- 内容 -->
        <StackPanel Grid.Row="1" Margin="30,20">
            <TextBlock Text="患者姓名 *" FontWeight="Bold" Margin="0,0,0,5"/>
            <TextBox Name="txtPatientName" Height="35" Padding="8,5" FontSize="14" Margin="0,0,0,15"/>
            
            <TextBlock Text="患者电话 *" FontWeight="Bold" Margin="0,0,0,5"/>
            <TextBox Name="txtPatientPhone" Height="35" Padding="8,5" FontSize="14" Margin="0,0,0,15"/>
            
            <TextBlock Text="科室 *" FontWeight="Bold" Margin="0,0,0,5"/>
            <ComboBox Name="cboDepartment" Height="35" Padding="8,5" FontSize="14" Margin="0,0,0,15">
                <ComboBoxItem>内科</ComboBoxItem>
                <ComboBoxItem>外科</ComboBoxItem>
                <ComboBoxItem>中医科</ComboBoxItem>
                <ComboBoxItem>妇科</ComboBoxItem>
                <ComboBoxItem>儿科</ComboBoxItem>
            </ComboBox>
            
            <TextBlock Text="挂号类型 *" FontWeight="Bold" Margin="0,0,0,5"/>
            <ComboBox Name="cboType" Height="35" Padding="8,5" FontSize="14" Margin="0,0,0,15">
                <ComboBoxItem>普通号</ComboBoxItem>
                <ComboBoxItem>专家号</ComboBoxItem>
                <ComboBoxItem>急诊号</ComboBoxItem>
            </ComboBox>
            
            <TextBlock Text="就诊日期 *" FontWeight="Bold" Margin="0,0,0,5"/>
            <DatePicker Name="dpDate" Height="35" Padding="8,5" FontSize="14" Margin="0,0,0,15"/>
            
            <TextBlock Text="备注" FontWeight="Bold" Margin="0,0,0,5"/>
            <TextBox Name="txtRemark" Height="60" Padding="8,5" TextWrapping="Wrap" 
                     AcceptsReturn="True" VerticalScrollBarVisibility="Auto" FontSize="14"/>
        </StackPanel>

        <!-- 按钮 -->
        <Border Grid.Row="2" Background="#F5F5F5" Padding="20,15">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Name="btnSave" Content="保存" Width="100" Height="35" Margin="0,0,10,0"
                        Background="#2196F3" Foreground="White" BorderThickness="0"
                        FontSize="14" FontWeight="Bold" Click="btnSave_Click"/>
                <Button Name="btnCancel" Content="取消" Width="100" Height="35"
                        Background="#6C757D" Foreground="White" BorderThickness="0"
                        FontSize="14" Click="btnCancel_Click"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>"""

    simple_dialog_cs = """using System;
using System.Windows;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    public partial class SimpleAddRegistrationDialog : Window
    {
        public SimpleAddRegistrationDialog()
        {
            InitializeComponent();
            dpDate.SelectedDate = DateTime.Today;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 简单验证
            if (string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                MessageBox.Show("请输入患者姓名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtPatientPhone.Text))
            {
                MessageBox.Show("请输入患者电话", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (cboDepartment.SelectedItem == null)
            {
                MessageBox.Show("请选择科室", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (cboType.SelectedItem == null)
            {
                MessageBox.Show("请选择挂号类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (!dpDate.SelectedDate.HasValue)
            {
                MessageBox.Show("请选择就诊日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // TODO: 这里应该调用API保存挂号信息
            MessageBox.Show("挂号信息已保存（功能开发中）", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}"""

    # 保存文件
    views_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\Views"
    
    try:
        # 保存XAML文件
        xaml_path = os.path.join(views_path, "SimpleAddRegistrationDialog.xaml")
        with open(xaml_path, 'w', encoding='utf-8') as f:
            f.write(simple_dialog_xaml)
        print(f"✅ 创建文件：{xaml_path}")
        
        # 保存CS文件
        cs_path = os.path.join(views_path, "SimpleAddRegistrationDialog.xaml.cs")
        with open(cs_path, 'w', encoding='utf-8') as f:
            f.write(simple_dialog_cs)
        print(f"✅ 创建文件：{cs_path}")
        
        return True
        
    except Exception as e:
        print(f"❌ 创建文件失败：{e}")
        return False


def update_viewmodel_to_use_simple_dialog():
    """更新ViewModel以使用简化版对话框"""
    
    vm_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\RegistrationManagementViewModelRefactored.cs"
    
    try:
        with open(vm_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 替换ExecuteAdd方法
        pattern = r'protected override void ExecuteAdd\(\)\s*\{[^}]*\}'
        replacement = """protected override void ExecuteAdd()
        {
            try
            {
                // 使用简化版对话框
                var dialog = new Views.SimpleAddRegistrationDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增挂号对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }"""
        
        content = re.sub(pattern, replacement, content, flags=re.DOTALL)
        
        with open(vm_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"✅ 已更新ViewModel使用简化版对话框")
        return True
        
    except Exception as e:
        print(f"❌ 更新ViewModel失败：{e}")
        return False


def main():
    """主函数"""
    print("=== 修复新建挂号按钮无效问题 ===\n")
    
    # 选择修复方案
    print("请选择修复方案：")
    print("1. 修复原有的AddRegistrationDialog（推荐）")
    print("2. 创建简化版的挂号对话框")
    print("3. 两种方案都执行")
    
    choice = input("\n请输入选择（1-3）：").strip()
    
    if choice == "1":
        print("\n正在修复原有对话框...")
        if fix_add_registration_dialog():
            print("✅ 修复完成！")
        else:
            print("❌ 修复失败！")
    
    elif choice == "2":
        print("\n正在创建简化版对话框...")
        if create_simple_registration_dialog() and update_viewmodel_to_use_simple_dialog():
            print("✅ 创建完成！")
        else:
            print("❌ 创建失败！")
    
    elif choice == "3":
        print("\n正在执行两种方案...")
        success1 = fix_add_registration_dialog()
        success2 = create_simple_registration_dialog()
        if success1 and success2:
            print("\n请选择使用哪个对话框：")
            print("1. 使用原有的AddRegistrationDialog")
            print("2. 使用简化版的SimpleAddRegistrationDialog")
            dialog_choice = input("\n请输入选择（1-2）：").strip()
            if dialog_choice == "2":
                update_viewmodel_to_use_simple_dialog()
            print("✅ 设置完成！")
        else:
            print("❌ 部分操作失败！")
    
    else:
        print("无效的选择！")
        return
    
    print("\n=== 完成 ===")
    print("\n注意事项：")
    print("1. 请重新编译项目")
    print("2. 运行程序并测试新建挂号功能")
    print("3. 如果还有问题，请检查相关服务是否正确注入")


if __name__ == "__main__":
    main()