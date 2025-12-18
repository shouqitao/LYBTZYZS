using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// PatientsController集成测试
    /// Issue #2231: WebAPI集成测试和UAT验证
    /// </summary>
    public class PatientsControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testPatient1Id;
        private Guid _testPatient2Id;
        private const string BaseUrl = "/api/v1/patients";

        public PatientsControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        protected override void SeedBasicTestData(AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试患者数据
            _testPatient1Id = Guid.NewGuid();
            _testPatient2Id = Guid.NewGuid();

            var testPatients = new List<Patient>
            {
                new Patient
                {
                    Id = _testPatient1Id,
                    Name = "张三",
                    PinYinCode = "zs",
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1990, 1, 1),
                    PhoneNumber = "13800138000",
                    IdNumber = "110101199001011234",
                    Address = "北京市朝阳区",
                    EmergencyContactName = "李四",
                    EmergencyContactPhone = "13900139000",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    Id = _testPatient2Id,
                    Name = "李四",
                    PinYinCode = "ls",
                    Gender = Gender.Female,
                    BirthDate = new DateTime(1992, 5, 15),
                    PhoneNumber = "13800138001",
                    IdNumber = "110101199205151234",
                    Address = "上海市浦东新区",
                    EmergencyContactName = "王五",
                    EmergencyContactPhone = "13900139001",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Set<Patient>().AddRange(testPatients);
            context.SaveChanges();
        }

        #region GetList Tests

        [Fact]
        public async Task GetList_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetList_WithDefaultPagination_ShouldReturnPagedResults()
        {
            // Arrange

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PatientDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountGreaterThan(0);
            result.Data.TotalCount.Should().BeGreaterThan(0);
            result.Data.CurrentPage.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetList_WithKeywordSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            // Use inherited Client with authentication
            var keyword = "张三";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&keyword={keyword}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PatientDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data.Items.Should().Contain(p => p.Name.Contains(keyword));
        }

        [Fact]
        public async Task GetList_WithInvalidPageNumber_ShouldReturnBadRequest()
        {
            // Arrange
            // Use inherited Client with authentication

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=0&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithExistingId_ShouldReturnPatient()
        {
            // Arrange
            // Use inherited Client with authentication

            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{_testPatient1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_testPatient1Id);
            result.Data.Name.Should().Be("张三");
        }

        [Fact]
        public async Task GetById_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            // Use inherited Client with authentication
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{BaseUrl}/{_testPatient1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidData_ShouldCreatePatient()
        {
            // Arrange
            // Use inherited Client with authentication
            var newPatient = new PatientDetailDto
            {
                Name = "王五",
                PinYinCode = "ww",
                Gender = Gender.Male,
                BirthDate = new DateTime(1985, 3, 20),
                PhoneNumber = "13800138002",
                IdNumber = "110101198503201234",
                Address = "广州市天河区",
                EmergencyContactName = "赵六",
                EmergencyContactPhone = "13900139002"
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, newPatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().NotBeEmpty();
            result.Data.Name.Should().Be("王五");
            result.Data.PhoneNumber.Should().Be("13800138002");
        }

        [Fact]
        public async Task Add_WithDuplicatePhoneNumber_ShouldReturnBadRequest()
        {
            // Arrange
            // Use inherited Client with authentication
            var duplicatePatient = new PatientDetailDto
            {
                Name = "重复患者",
                PinYinCode = "cf",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000", // 与测试数据中的张三重复
                IdNumber = "110101199001019999",
                Address = "测试地址"
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, duplicatePatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Contain("已存在");
        }

        [Fact]
        public async Task Add_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var newPatient = new PatientDetailDto
            {
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138999"
            };

            // Act
            var response = await client.PostAsJsonAsync(BaseUrl, newPatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldUpdatePatient()
        {
            // Arrange
            // Use inherited Client with authentication
            var updatedPatient = new PatientDetailDto
            {
                Id = _testPatient1Id,
                Name = "张三(已更新)",
                PinYinCode = "zs",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                IdNumber = "110101199001011234",
                Address = "北京市海淀区(新地址)",
                EmergencyContactName = "李四",
                EmergencyContactPhone = "13900139000"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{_testPatient1Id}", updatedPatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("张三(已更新)");
            result.Data.Address.Should().Be("北京市海淀区(新地址)");
        }

        [Fact]
        public async Task Update_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            // Use inherited Client with authentication
            var nonExistingId = Guid.NewGuid();
            var updatedPatient = new PatientDetailDto
            {
                Id = nonExistingId,
                Name = "不存在的患者",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138999"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", updatedPatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var updatedPatient = new PatientDetailDto
            {
                Id = _testPatient1Id,
                Name = "测试更新",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000"
            };

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{_testPatient1Id}", updatedPatient);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithExistingId_ShouldSoftDeletePatient()
        {
            // Arrange
            // Use inherited Client with authentication

            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{_testPatient2Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证患者已被软删除（无法再查询到）
            var getResponse = await Client.GetAsync($"{BaseUrl}/{_testPatient2Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            // Use inherited Client with authentication
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.DeleteAsync($"{BaseUrl}/{_testPatient1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Phase 3 Optimization Verification

        [Fact]
        public async Task GetList_ShouldReturnAgeCalculatedInController()
        {
            // Arrange
            // Use inherited Client with authentication

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PatientDetailDto>>>();
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();

            // 验证Age属性已正确计算（从Entity的计算属性复制到DTO）
            foreach (var patient in result.Data.Items)
            {
                patient.Age.Should().BeGreaterThanOrEqualTo(0);

                // 验证年龄计算合理性（如1990年出生的患者应该在30-40岁之间）
                if (patient.BirthDate.HasValue && patient.BirthDate.Value.Year == 1990)
                {
                    patient.Age.Should().BeInRange(30, 40);
                }
            }
        }

        #endregion
    }
}
