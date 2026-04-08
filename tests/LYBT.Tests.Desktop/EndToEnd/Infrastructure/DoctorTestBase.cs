using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

/// <summary>
/// 医生职责测试基类
/// 
/// 职责范围：
/// - 医案管理（创建、编辑、完成、状态转换）
/// - 诊疗流程（诊断录入、处方开具）
/// - 患者队列（接诊、队列过滤）
/// - 历史查询（病史查看）
/// </summary>
public abstract class DoctorTestBase : WebApiE2ETestBase
{
    protected new async Task<LoginResponse> LoginAsDoctorAsync()
    {
        var username = Configuration["TestCredentials:Doctor:Username"] ?? "doctor";
        var password = Configuration["TestCredentials:Doctor:Password"] ?? "DoctorPass123!";
        
        return await LoginAsAsync(username, password);
    }

    protected async Task<bool> VerifyDoctorMedicalCaseAccess()
    {
        var response = await MedicalCaseApi.GetMedicalCasesAsync();
        return response.Success;
    }
}
