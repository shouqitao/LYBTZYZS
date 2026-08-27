// ---------------------------------------------------------------------------
// PermissionBoundaryE2ETests — 跨角色权限验证（403 断言）
// 权限矩阵（docs/01-product/04-permissions.md 对齐）：
//   用户管理 AdminOrSuperAdmin；药材创建 AdminOrSuperAdmin；医案创建 DoctorOnly；
//   诊断 AdminOrSuperAdmin；配置 SysAdminOnly；部署 SysAdminOnly
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Tests.Desktop.E2E.BoundaryFlow;

[Collection("E2ELocal")]
public class PermissionBoundaryE2ETests : E2ETestBase
{
    [Fact]
    public async Task Doctor_CannotListUsers_Forbidden()
    {
        await LoginAsDoctorAsync();

        await AssertForbiddenAsync(() => IdentityApi.GetUsersAsync());
    }

    [Fact]
    public async Task Doctor_CannotCreateUser_Forbidden()
    {
        await LoginAsDoctorAsync();

        await AssertForbiddenAsync(() => IdentityApi.CreateUserAsync(new LYBT.Shared.Models.Contracts.Users.UserInputDto
        {
            UserName = UniqueUsername(),
            RealName = "越权用户",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        }));
    }

    [Fact]
    public async Task Receptionist_CannotCreateHerb_Forbidden()
    {
        await LoginAsReceptionistAsync();

        await AssertForbiddenAsync(() => HerbsApi.CreateHerbAsync(new HerbInputDto
        {
            Name = UniqueName("越权药"),
            Unit = "克",
            Price = 1m
        }));
    }

    [Fact]
    public async Task Receptionist_CannotCreateMedicalCase_Forbidden()
    {
        await LoginAsReceptionistAsync();

        // 医案创建 DoctorOnly
        await AssertForbiddenAsync(() => MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task Doctor_CannotAccessDiagnostics_Forbidden()
    {
        await LoginAsDoctorAsync();

        await AssertForbiddenAsync(() => DiagnosticsApi.GetLoggingStatusAsync());
    }

    [Fact]
    public async Task Admin_CannotAccessConfiguration_Forbidden()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync("/api/v1/configuration");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Doctor_CanReadPatients_Allowed()
    {
        await LoginAsDoctorAsync();

        var result = await PatientsApi.GetPatientsAsync();

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task Receptionist_CanReadPatients_Allowed()
    {
        await LoginAsReceptionistAsync();

        var result = await PatientsApi.GetPatientsAsync();

        Assert.True(result.Success, result.Message);
    }
}
