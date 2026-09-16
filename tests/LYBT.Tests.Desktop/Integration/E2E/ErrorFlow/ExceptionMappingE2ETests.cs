// ---------------------------------------------------------------------------
// ExceptionMappingE2ETests — US-ERR-006（验证异常映射）+ US-ERR-007（异常体系）全链路
// 真实链路：原始 HTTP → LocalWebAPI（Kestrel）→ 控制器/管道 → LocalDB
// ---------------------------------------------------------------------------
// 响应映射实测（**本地模式 SharedHost**，与 Server 端 X-3 后契约不同）：
//   · 模型校验失败（[ApiController] 自动校验）→ 400 ProblemDetails（RFC7807：title/status/errors）
//   · 业务失败（NotFound/BusinessFail）        → ApiResponse { success:false, message, errors, requestId }
//   · 认证缺失 → 401（空体）；角色不足 → 403（空体，授权中间件短路）
//   · 业务规则违反（BusinessFail 白名单等）    → 422 + ApiResponse
//   · 本地 SharedHost 异常兜底仍写 ApiResponse（未纳入 X-3）；
//     Server 端（Remote WebAPI）异常路径已统一 ProblemDetails（见 ADR-0020 X-3）
//
// 已知缺口（本次不修，登记见 13c）：
//   ① 本地模式**无 409 生产者**：ErrorCode 中 ConcurrencyConflict/MedicalCaseVersionConflict/
//      MedicalCaseLocked/PatientPhoneDuplicate 均映射 409，但前三者无代码抛出；电话重复查重
//      （PatientPhoneDuplicate，US-PAT-003/004）因 PhoneNumber 经 AES-GCM 非确定性加密，
//      SQL 层等值比较恒不命中 → 重复电话不阻断（实测两次同号创建均 201）。
//   ② 处理器内 FluentValidation 异常经 SharedHost.UseExceptionHandler 统一落 500（非 400）。
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.ErrorFlow;

[Collection("E2ELocal")]
public class ExceptionMappingE2ETests : E2ETestBase
{
    /// <summary>ApiResponse 规范：success/message/data/errors/timestamp/requestId 六字段齐备。</summary>
    private static void AssertApiResponseShape(JsonElement json, bool expectedSuccess)
    {
        Assert.Equal(expectedSuccess, json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));
        Assert.True(json.TryGetProperty("data", out _), "缺少 data 字段");
        Assert.True(json.TryGetProperty("errors", out _), "缺少 errors 字段");
        Assert.True(json.GetProperty("timestamp").GetInt64() > 0, "timestamp 非法");
        Assert.True(json.TryGetProperty("requestId", out _), "缺少 requestId 字段");
    }

    /// <summary>US-ERR-006：模型校验失败 → 400，响应体含字段级 errors 字典。</summary>
    [Fact]
    public async Task Validation_InvalidPayload_Returns400WithFieldErrors()
    {
        await LoginAsAdminAsync();

        // 患者姓名 [Required] 缺失 → [ApiController] 管道自动校验短路
        var response = await Client.PostAsJsonAsync("/api/v1/patients", new { phoneNumber = UniquePhone() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(400, json.GetProperty("status").GetInt32());
        var errors = json.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.TryGetProperty("Name", out var nameErrors), json.ToString());
        Assert.Contains("患者姓名不能为空", nameErrors[0].GetString());
    }

    /// <summary>US-ERR-006：资源不存在 → 404 + ApiResponse 失败体（含 requestId）。</summary>
    [Fact]
    public async Task NotFound_MissingResource_Returns404WithApiResponse()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        AssertApiResponseShape(json, expectedSuccess: false);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("requestId").GetString()));
    }

    /// <summary>US-ERR-006：未认证 → 401。</summary>
    [Fact]
    public async Task Unauthorized_AnonymousRequest_Returns401()
    {
        ClearAuth();

        var response = await Client.GetAsync("/api/v1/patients?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>US-ERR-007：已认证但角色不足 → 403（医生访问用户管理与系统配置均被拒）。</summary>
    [Fact]
    public async Task Forbidden_InsufficientRole_Returns403()
    {
        await LoginAsDoctorAsync();

        var usersResponse = await Client.PostAsJsonAsync("/api/v1/users", new { });
        var configurationResponse = await Client.GetAsync("/api/v1/configuration");

        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, configurationResponse.StatusCode);
    }

    /// <summary>
    /// US-ERR-007：业务规则违反 → 422 + ApiResponse 失败体。
    /// 以配置白名单校验为例（同时是 US-CFG-006 的诊所节不可写证据，详见文件头备注）。
    /// </summary>
    [Fact]
    public async Task BusinessRuleViolation_Returns422WithApiResponse()
    {
        await LoginAsSysadminAsync();

        var response = await Client.PutAsJsonAsync(
            "/api/v1/configuration/sections/ClinicSettings",
            new Dictionary<string, string> { ["Name"] = "E2E 诊所" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        AssertApiResponseShape(json, expectedSuccess: false);
        Assert.Contains("白名单", json.GetProperty("message").GetString());
    }

    /// <summary>US-ERR-006：成功响应同样遵循 ApiResponse 规范（业务失败与成功同构）。</summary>
    [Fact]
    public async Task Success_ConformsToApiResponseShape()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync("/api/v1/patients?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        AssertApiResponseShape(json, expectedSuccess: true);
        Assert.Equal(JsonValueKind.Object, json.GetProperty("data").ValueKind);
    }
}
