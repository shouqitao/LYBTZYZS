using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Patients;

/// <summary>
/// 患者管理模块集成测试。
/// 验证完整HTTP管线: Controller -> PatientService -> Repository -> DB。
/// 授权策略: DoctorOrAdmin (医生和管理员均可访问)。
/// </summary>
[Collection("ServerIntegration")]
public class PatientIntegrationTests
{
    private readonly WebApiFixture _fixture;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PatientIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    #region Create Patient

    [Fact]
    public async Task CreatePatient_WithValidData_ShouldPersist()
    {
        // Arrange
        var request = new PatientInputDto
        {
            Name = "集成测试患者_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male,
            BirthDate = new DateTime(1985, 3, 15),
            PhoneNumber = "13800001001",
            Address = "北京市朝阳区"
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"创建患者应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be(request.Name);
        body.Data.Gender.Should().Be(Gender.Male);
        body.Data.PhoneNumber.Should().Be("13800001001");
        body.Data.Address.Should().Be("北京市朝阳区");
        body.Data.Id.Should().NotBe(Guid.Empty, "应生成有效ID");
        body.Data.PinYinCode.Should().NotBeNullOrWhiteSpace("应自动生成拼音码");
    }

    [Fact]
    public async Task CreatePatient_WithFullData_ShouldPersistAllFields()
    {
        // Arrange - 包含所有可选字段
        var request = new PatientInputDto
        {
            Name = "完整患者_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Female,
            BirthDate = new DateTime(1990, 8, 20),
            PhoneNumber = "13800001002",
            Address = "上海市浦东新区",
            AllergyHistory = "青霉素过敏",
            MedicalHistory = "2020年阑尾炎手术",
            EmergencyContactName = "家属张三",
            EmergencyContactPhone = "13900001001",
            EmergencyContactRelation = "配偶"
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.AllergyHistory.Should().Be("青霉素过敏");
        body.Data.MedicalHistory.Should().Be("2020年阑尾炎手术");
        body.Data.EmergencyContactName.Should().Be("家属张三");
        body.Data.Age.Should().NotBeNull("应根据出生日期计算年龄");
        body.Data.Age.Should().BeGreaterThan(30);
    }

    [Fact]
    public async Task CreatePatient_WithoutName_ShouldReturn400()
    {
        // Arrange - Name 是必填字段
        var request = new PatientInputDto
        {
            Name = "", // 空姓名
            Gender = Gender.Male
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Patients

    [Fact]
    public async Task GetPatients_ShouldReturnPagedList()
    {
        // Arrange - 先确保至少有一个患者
        var createRequest = new PatientInputDto
        {
            Name = "列表测试_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male
        };
        await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);

        // Act
        var response = await _fixture.AdminClient
            .GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<PatientListDto>>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeEmpty("至少有一个患者");
        body.Data.TotalCount.Should().BeGreaterOrEqualTo(1);
        body.Data.PageSize.Should().Be(20, "默认每页20条");
    }

    [Fact]
    public async Task GetPatient_ById_ShouldReturnDetail()
    {
        // Arrange - 先创建一个患者
        var createRequest = new PatientInputDto
        {
            Name = "详情测试_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Female,
            BirthDate = new DateTime(1988, 6, 10),
            PhoneNumber = "13800001003"
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{patientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(patientId);
        body.Data.Name.Should().Be(createRequest.Name);
        body.Data.Gender.Should().Be(Gender.Female);
        body.Data.PhoneNumber.Should().Be("13800001003");
    }

    [Fact]
    public async Task GetPatient_NonExistentId_ShouldReturn404()
    {
        // Arrange & Act
        var response = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Search

    [Fact]
    public async Task SearchPatient_ByName_ShouldReturnMatches()
    {
        // Arrange - 创建一个带唯一名字的患者
        var uniqueName = "搜索王_" + Guid.NewGuid().ToString("N")[..6];
        var createRequest = new PatientInputDto
        {
            Name = uniqueName,
            Gender = Gender.Male
        };
        await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);

        // Act - 使用关键字搜索
        var keyword = uniqueName.Substring(0, 4);
        var response = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients?keyword={Uri.EscapeDataString(keyword)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<PatientListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().Contain(
            p => p.Name.Contains(keyword),
            "搜索结果应包含匹配的患者");
    }

    [Fact]
    public async Task SearchPatient_ByPinYinCode_ShouldReturnMatches()
    {
        // Arrange - 创建患者，系统自动生成拼音码
        var createRequest = new PatientInputDto
        {
            Name = "张伟_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var pinyin = created!.Data!.PinYinCode;

        // Act - 使用拼音码搜索 (取前两个字符)
        if (!string.IsNullOrWhiteSpace(pinyin) && pinyin.Length >= 2)
        {
            var pinyinKeyword = pinyin.Substring(0, 2);
            var response = await _fixture.AdminClient
                .GetAsync($"/api/v1/patients?keyword={pinyinKeyword}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<PagedResult<PatientListDto>>>(JsonOptions);
            body!.Success.Should().BeTrue();
            body.Data!.Items.Should().NotBeEmpty("拼音码搜索应返回结果");
        }
    }

    #endregion

    #region Update Patient

    [Fact]
    public async Task UpdatePatient_ShouldModifyFields()
    {
        // Arrange - 先创建患者
        var createRequest = new PatientInputDto
        {
            Name = "更新前_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male,
            PhoneNumber = "13800001004"
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // Act - 更新信息
        var updateRequest = new PatientInputDto
        {
            Id = patientId,
            Name = "更新后姓名",
            Gender = Gender.Female,
            PhoneNumber = "13900001004",
            Address = "更新后地址",
            AllergyHistory = "花粉过敏"
        };
        var updateResponse = await _fixture.AdminClient
            .PutAsJsonAsync($"/api/v1/patients/{patientId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        updated!.Success.Should().BeTrue();
        updated.Data!.Name.Should().Be("更新后姓名");
        updated.Data.Gender.Should().Be(Gender.Female);
        updated.Data.PhoneNumber.Should().Be("13900001004");
        updated.Data.Address.Should().Be("更新后地址");
        updated.Data.AllergyHistory.Should().Be("花粉过敏");

        // Verify: 重新获取确认持久化
        var getResponse = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{patientId}");
        var fetched = await getResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        fetched!.Data!.Name.Should().Be("更新后姓名", "更新应已持久化到数据库");
    }

    [Fact]
    public async Task UpdatePatient_NameChange_ShouldRegeneratePinYinCode()
    {
        // Arrange - 创建患者
        var createRequest = new PatientInputDto
        {
            Name = "原始名字",
            Gender = Gender.Male
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;
        var originalPinyin = created.Data.PinYinCode;

        // Act - 修改姓名
        var updateRequest = new PatientInputDto
        {
            Id = patientId,
            Name = "完全不同"
        };
        var updateResponse = await _fixture.AdminClient
            .PutAsJsonAsync($"/api/v1/patients/{patientId}", updateRequest);

        // Assert - 拼音码应重新生成
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        updated!.Data!.PinYinCode.Should().NotBe(originalPinyin,
            "姓名变化后拼音码应重新生成");
    }

    #endregion

    #region Delete Patient

    [Fact]
    public async Task DeletePatient_ShouldSoftDelete()
    {
        // Arrange - 先创建患者
        var createRequest = new PatientInputDto
        {
            Name = "待删除患者_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // Act - 删除患者
        var deleteResponse = await _fixture.AdminClient
            .DeleteAsync($"/api/v1/patients/{patientId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify: 删除后获取应404
        var getResponse = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{patientId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "软删除后应查不到该患者");
    }

    #endregion

    #region Restore Patient

    [Fact]
    public async Task Restore_SoftDeletedPatient_ShouldMakeAccessibleAgain()
    {
        // Arrange - 创建并删除患者
        var createRequest = new PatientInputDto
        {
            Name = "待恢复患者_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Female
        };
        var createResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // 软删除
        await _fixture.AdminClient.DeleteAsync($"/api/v1/patients/{patientId}");

        // 确认已删除
        var getAfterDelete = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{patientId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act - 恢复患者
        var restoreResponse = await _fixture.AdminClient
            .PostAsync($"/api/v1/patients/{patientId}/restore", null);

        // Assert
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await restoreResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue($"恢复应成功, 实际消息: {body.Message}");
        body.Data!.Id.Should().Be(patientId);

        // Verify: 恢复后可以正常获取
        var getAfterRestore = await _fixture.AdminClient
            .GetAsync($"/api/v1/patients/{patientId}");
        getAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Batch Delete

    [Fact]
    public async Task BatchDelete_MultiplePatients_ShouldSoftDeleteAll()
    {
        // Arrange - 创建3个患者
        var patientIds = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var request = new PatientInputDto
            {
                Name = $"批删患者{i}_" + Guid.NewGuid().ToString("N")[..4],
                Gender = Gender.Male
            };
            var response = await _fixture.AdminClient
                .PostAsJsonAsync("/api/v1/patients", request);
            var created = await response.Content
                .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
            patientIds.Add(created!.Data!.Id);
        }

        // Act - 批量删除
        var batchRequest = new { Ids = patientIds };
        var batchResponse = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients/batch-delete", batchRequest);

        // Assert
        batchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify: 删除后逐个查询应404
        foreach (var id in patientIds)
        {
            var getResponse = await _fixture.AdminClient
                .GetAsync($"/api/v1/patients/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"批量删除后患者 {id} 应查不到");
        }
    }

    [Fact]
    public async Task BatchDelete_EmptyList_ShouldReturn400()
    {
        // Arrange
        var batchRequest = new { Ids = new List<Guid>() };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients/batch-delete", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task GetPatients_WithDoctorToken_ShouldReturn200()
    {
        // Arrange & Act - DoctorOrAdmin策略: Doctor也应有权限
        var response = await _fixture.DoctorClient
            .GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Doctor角色应能访问患者列表 (DoctorOrAdmin策略)");
    }

    [Fact]
    public async Task CreatePatient_WithDoctorToken_ShouldSucceed()
    {
        // Arrange - Doctor也可以创建患者
        var request = new PatientInputDto
        {
            Name = "医生创建_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male
        };

        // Act
        var response = await _fixture.DoctorClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Doctor角色应能创建患者");
    }

    [Fact]
    public async Task GetPatients_WithoutToken_ShouldReturn401()
    {
        // Arrange & Act
        var response = await _fixture.AnonymousClient
            .GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Pagination

    [Fact]
    public async Task GetPatients_WithPagination_ShouldRespectPageSize()
    {
        // Arrange - 确保至少有3个患者
        for (int i = 0; i < 3; i++)
        {
            var request = new PatientInputDto
            {
                Name = $"分页测试{i}_" + Guid.NewGuid().ToString("N")[..4],
                Gender = Gender.Male
            };
            await _fixture.AdminClient
                .PostAsJsonAsync("/api/v1/patients", request);
        }

        // Act - 请求第1页，每页2条
        var response = await _fixture.AdminClient
            .GetAsync("/api/v1/patients?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<PatientListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Count.Should().BeLessOrEqualTo(2, "每页最多2条");
        body.Data.PageSize.Should().Be(2);
        body.Data.CurrentPage.Should().Be(1);
        if (body.Data.TotalCount > 2)
        {
            body.Data.TotalPages.Should().BeGreaterThan(1, "总数超过pageSize时应有多页");
        }
    }

    [Fact]
    public async Task GetPatients_InvalidPagination_ShouldReturn400()
    {
        // Act - page=0 是无效参数
        var response = await _fixture.AdminClient
            .GetAsync("/api/v1/patients?page=0&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "page=0应返回400");
    }

    #endregion

    #region Ownership Check

    [Fact]
    public async Task AdminUpdate_DoctorCreatedPatient_ShouldSucceed()
    {
        // Arrange - Doctor创建一个患者
        var createRequest = new PatientInputDto
        {
            Name = "Doctor所有_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male
        };
        var createResponse = await _fixture.DoctorClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // Act - Admin更新Doctor创建的患者 (Admin应绕过所有权检查)
        var updateRequest = new PatientInputDto
        {
            Id = patientId,
            Name = "Admin已修改_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Male
        };
        var updateResponse = await _fixture.AdminClient
            .PutAsJsonAsync($"/api/v1/patients/{patientId}", updateRequest);

        // Assert - Admin应能更新任何患者
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Admin应能更新Doctor创建的患者");
    }

    [Fact]
    public async Task AdminDelete_DoctorCreatedPatient_ShouldSucceed()
    {
        // Arrange - Doctor创建一个患者
        var createRequest = new PatientInputDto
        {
            Name = "Doctor待删_" + Guid.NewGuid().ToString("N")[..6],
            Gender = Gender.Female
        };
        var createResponse = await _fixture.DoctorClient
            .PostAsJsonAsync("/api/v1/patients", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        var patientId = created!.Data!.Id;

        // Act - Admin删除Doctor创建的患者
        var deleteResponse = await _fixture.AdminClient
            .DeleteAsync($"/api/v1/patients/{patientId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Admin应能删除Doctor创建的患者");
    }

    #endregion

    #region Age Calculation

    [Fact]
    public async Task CreatePatient_WithBirthDate_ShouldCalculateAge()
    {
        // Arrange - 1990年出生
        var request = new PatientInputDto
        {
            Name = "年龄计算_" + Guid.NewGuid().ToString("N")[..6],
            BirthDate = new DateTime(1990, 1, 1)
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body!.Data!.Age.Should().NotBeNull("有BirthDate时应计算Age");
        body.Data.Age.Should().BeGreaterOrEqualTo(35, "1990年出生应至少35岁");
        body.Data.Age.Should().BeLessOrEqualTo(37, "1990年出生应不超过37岁");
    }

    [Fact]
    public async Task CreatePatient_WithoutBirthDate_AgeShouldBeNull()
    {
        // Arrange - 不提供出生日期
        var request = new PatientInputDto
        {
            Name = "无年龄_" + Guid.NewGuid().ToString("N")[..6]
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        body!.Data!.BirthDate.Should().BeNull();
        body.Data.Age.Should().BeNull("无BirthDate时Age应为null");
    }

    #endregion
}
