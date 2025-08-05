using System;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    public partial class SimpleAddRegistrationDialog : Window
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IRegistrationApiService? _registrationApiService;
        private readonly IPatientService? _patientService;
        private readonly IDoctorService? _doctorService;

        public SimpleAddRegistrationDialog(ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            InitializeComponent();
            dpDate.SelectedDate = DateTime.Today;
            
            // 尝试获取服务
            try
            {
                var app = Application.Current as Prism.DryIoc.PrismApplication;
                if (app?.Container != null)
                {
                    var container = app.Container;
                    _registrationApiService = container.Resolve(typeof(IRegistrationApiService)) as IRegistrationApiService;
                    _patientService = container.Resolve(typeof(IPatientService)) as IPatientService;
                    _doctorService = container.Resolve(typeof(IDoctorService)) as IDoctorService;
                }
            }
            catch
            {
                // 服务获取失败，将在使用时提示
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 简单验证
            if (string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                await _commonDialogService.ShowWarningAsync("请输入患者姓名", "提示");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtPatientPhone.Text))
            {
                await _commonDialogService.ShowWarningAsync("请输入患者电话", "提示");
                return;
            }
            
            if (cboDepartment.SelectedItem == null)
            {
                await _commonDialogService.ShowWarningAsync("请选择科室", "提示");
                return;
            }
            
            if (cboType.SelectedItem == null)
            {
                await _commonDialogService.ShowWarningAsync("请选择挂号类型", "提示");
                return;
            }
            
            if (!dpDate.SelectedDate.HasValue)
            {
                await _commonDialogService.ShowWarningAsync("请选择就诊日期", "提示");
                return;
            }

            // 检查服务是否可用
            if (_registrationApiService == null || _patientService == null || _doctorService == null)
            {
                await _commonDialogService.ShowErrorAsync("服务初始化失败，无法创建挂号", "错误");
                return;
            }

            try
            {
                // 1. 查找或创建患者
                Guid patientId;
                var patients = await _patientService.GetListAsync();
                var existingPatient = patients.FirstOrDefault(p => 
                    p.Name == txtPatientName.Text && 
                    p.PhoneNumber == txtPatientPhone.Text);

                if (existingPatient != null)
                {
                    patientId = existingPatient.Id;
                }
                else
                {
                    // 创建新患者
                    var newPatient = new PatientDetailDto
                    {
                        Id = Guid.NewGuid(),
                        Name = txtPatientName.Text,
                        PhoneNumber = txtPatientPhone.Text,
                        Gender = Gender.Male, // 默认性别，实际应该从界面获取
                        BirthDate = DateTime.Now.AddYears(-30), // 默认30岁
                        Age = 30,
                        IsActive = true,
                        CreateTime = DateTime.Now
                    };

                    var createResult = await _patientService.AddAsync(newPatient);
                    if (!createResult.IsSuccess)
                    {
                        await _commonDialogService.ShowErrorAsync($"创建患者失败: {createResult.ErrorMessage}", "错误");
                        return;
                    }
                    patientId = newPatient.Id;
                }

                // 2. 获取医生（简化处理，获取第一个医生）
                var department = (cboDepartment.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "中医科";
                var doctorsResult = await _doctorService.GetByDepartmentAsync(department);
                
                Guid doctorId;
                if (doctorsResult.IsSuccess && doctorsResult.Data != null && doctorsResult.Data.Any())
                {
                    doctorId = doctorsResult.Data.First().Id;
                }
                else
                {
                    // 如果没有找到医生，使用一个默认的医生ID
                    // 实际应用中应该提示用户选择医生
                    await _commonDialogService.ShowWarningAsync($"科室 {department} 暂无医生，将使用默认医生", "提示");
                    doctorId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // 默认医生ID
                }

                // 3. 转换挂号类型
                var registrationType = (cboType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() switch
                {
                    "普通号" => RegistrationType.Regular,
                    "专家号" => RegistrationType.Expert,
                    "急诊号" => RegistrationType.Emergency,
                    _ => RegistrationType.Regular
                };

                // 4. 计算挂号费
                decimal registrationFee = registrationType switch
                {
                    RegistrationType.Expert => 50,
                    RegistrationType.Emergency => 20,
                    _ => 10
                };

                // 5. 创建挂号记录
                var registration = new RegistrationCreateDto
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Department = department,
                    RegistrationType = registrationType,
                    RegistrationFee = registrationFee,
                    AppointmentDate = dpDate.SelectedDate.Value,
                    AppointmentTimeSlot = "上午", // 默认上午
                    IsPaid = false,
                    Remark = txtRemark.Text
                };

                // 6. 调用API创建挂号
                var response = await _registrationApiService.CreateRegistrationAsync(registration);
                if (response.IsSuccessStatusCode)
                {
                    await _commonDialogService.ShowInformationAsync("挂号创建成功", "成功");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    var error = response.Error?.Content ?? "创建挂号失败";
                    await _commonDialogService.ShowErrorAsync(error, "错误");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"创建挂号时发生错误: {ex.Message}", "错误");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}