using FluentAssertions;
using LYBT.Tests.Configuration;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LYBT.IntegrationTests.Controllers
{
    /// <summary>
    /// PatientsController 集成测试
    /// 测试患者管理API的端到端功能
    /// </summary>
    public class PatientsControllerTests : IntegrationTestBase
    {
        public PatientsControllerTests()
            : base()
        {
        }

        #region GetPatients Tests

        [Fact]
        public async Task GetPatients_ShouldReturnPagedPatients()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/patients?page=1&pageSize=10");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().NotBeEmpty();
            apiResponse.Data.CurrentPage.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPatients_WithSearchKeyword_ShouldReturnFilteredPatients()
        {
            // Arrange
            await SeedTestDataAsync();
            var searchKeyword = "测试";

            // Act
            var response = await Client.GetAsync($"/api/patients?page=1&pageSize=10&searchKeyword={searchKeyword}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().AllSatisfy(patient => 
                patient.Name.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                patient.PhoneNumber.Contains(searchKeyword));
        }

        [Fact]
        public async Task GetPatients_WithGenderFilter_ShouldReturnFilteredPatients()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/patients?page=1&pageSize=10&gender=1"); // 1 = Male

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().AllSatisfy(patient => 
                patient.Gender.Should().Be(Gender.Male));
        }

        #endregion

        #region GetPatientById Tests

        [Fact]
        public async Task GetPatientById_WithValidId_ShouldReturnPatient()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient = await CreateTestPatientAsync();

            // Act
            var response = await Client.GetAsync($"/api/patients/{patient.Id}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Id.Should().Be(patient.Id);
            apiResponse.Data.Name.Should().Be(patient.Name);
        }

        [Fact]
        public async Task GetPatientById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/patients/{nonExistentId}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        #endregion

        #region CreatePatient Tests

        [Fact]
        public async Task CreatePatient_WithValidData_ShouldReturnCreatedPatient()
        {
            // Arrange
            var createPatientDto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000",
                Gender = Gender.Male,
                DateOfBirth = new DateTime(1990, 1, 1),
                Address = "北京市朝阳区",
                MedicalHistory = "无特殊病史"
            };

            var content = CreateJsonContent(createPatientDto);

            // Act
            var response = await Client.PostAsync("/api/patients", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Created);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Name.Should().Be(createPatientDto.Name);
            apiResponse.Data.PhoneNumber.Should().Be(createPatientDto.PhoneNumber);
            apiResponse.Data.Gender.Should().Be(createPatientDto.Gender);
            apiResponse.Data.DateOfBirth.Should().Be(createPatientDto.DateOfBirth);
            apiResponse.Data.Address.Should().Be(createPatientDto.Address);
            apiResponse.Data.MedicalHistory.Should().Be(createPatientDto.MedicalHistory);
            apiResponse.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreatePatient_WithDuplicatePhoneNumber_ShouldReturnConflict()
        {
            // Arrange
            await SeedTestDataAsync();
            var existingPatient = await CreateTestPatientAsync();
            
            var createPatientDto = new PatientCreateDto
            {
                Name = "李四",
                PhoneNumber = existingPatient.PhoneNumber, // 重复的手机号
                Gender = Gender.Female
            };

            var content = CreateJsonContent(createPatientDto);

            // Act
            var response = await Client.PostAsync("/api/patients", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task CreatePatient_WithInvalidPhoneNumber_ShouldReturnValidationError()
        {
            // Arrange
            var createPatientDto = new PatientCreateDto
            {
                Name = "王五",
                PhoneNumber = "invalid-phone", // 无效手机号
                Gender = Gender.Male
            };

            var content = CreateJsonContent(createPatientDto);

            // Act
            var response = await Client.PostAsync("/api/patients", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreatePatient_WithFutureBirthDate_ShouldReturnValidationError()
        {
            // Arrange
            var createPatientDto = new PatientCreateDto
            {
                Name = "赵六",
                PhoneNumber = "13600136000",
                Gender = Gender.Female,
                DateOfBirth = DateTime.Now.AddDays(1) // 未来日期
            };

            var content = CreateJsonContent(createPatientDto);

            // Act
            var response = await Client.PostAsync("/api/patients", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region UpdatePatient Tests

        [Fact]
        public async Task UpdatePatient_WithValidData_ShouldReturnUpdatedPatient()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient = await CreateTestPatientAsync();
            
            var updatePatientDto = new PatientUpdateDto
            {
                Id = patient.Id,
                Name = "更新后的张三",
                PhoneNumber = "13900139000",
                Address = "上海市浦东新区",
                MedicalHistory = "高血压病史"
            };

            var content = CreateJsonContent(updatePatientDto);

            // Act
            var response = await Client.PutAsync($"/api/patients/{patient.Id}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Id.Should().Be(patient.Id);
            apiResponse.Data.Name.Should().Be(updatePatientDto.Name);
            apiResponse.Data.PhoneNumber.Should().Be(updatePatientDto.PhoneNumber);
            apiResponse.Data.Address.Should().Be(updatePatientDto.Address);
            apiResponse.Data.MedicalHistory.Should().Be(updatePatientDto.MedicalHistory);
        }

        [Fact]
        public async Task UpdatePatient_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updatePatientDto = new PatientUpdateDto
            {
                Id = nonExistentId,
                Name = "不存在的患者"
            };

            var content = CreateJsonContent(updatePatientDto);

            // Act
            var response = await Client.PutAsync($"/api/patients/{nonExistentId}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdatePatient_WithDuplicatePhoneNumber_ShouldReturnConflict()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient1 = await CreateTestPatientAsync("患者1", "13800138001");
            var patient2 = await CreateTestPatientAsync("患者2", "13800138002");
            
            var updatePatientDto = new PatientUpdateDto
            {
                Id = patient1.Id,
                PhoneNumber = patient2.PhoneNumber // 使用另一个患者的手机号
            };

            var content = CreateJsonContent(updatePatientDto);

            // Act
            var response = await Client.PutAsync($"/api/patients/{patient1.Id}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
        }

        #endregion

        #region DeletePatient Tests

        [Fact]
        public async Task DeletePatient_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient = await CreateTestPatientAsync();

            // Act
            var response = await Client.DeleteAsync($"/api/patients/{patient.Id}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            // 验证患者已被删除
            var getResponse = await Client.GetAsync($"/api/patients/{patient.Id}");
            getResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeletePatient_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"/api/patients/{nonExistentId}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        #endregion

        #region SearchByPhoneNumber Tests

        [Fact]
        public async Task SearchByPhoneNumber_WithValidNumber_ShouldReturnMatchingPatient()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient = await CreateTestPatientAsync();

            // Act
            var response = await Client.GetAsync($"/api/patients/search-by-phone/{patient.PhoneNumber}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.PhoneNumber.Should().Be(patient.PhoneNumber);
        }

        [Fact]
        public async Task SearchByPhoneNumber_WithNonExistentNumber_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentPhone = "19999999999";

            // Act
            var response = await Client.GetAsync($"/api/patients/search-by-phone/{nonExistentPhone}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetPatientHistory Tests

        [Fact]
        public async Task GetPatientHistory_WithValidId_ShouldReturnPatientHistory()
        {
            // Arrange
            await SeedTestDataAsync();
            var patient = await CreateTestPatientAsync();

            // Act
            var response = await Client.GetAsync($"/api/patients/{patient.Id}/history");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<MedicalCaseDto>>();
            apiResponse.Data.Should().NotBeNull();
            // 初始历史记录应该为空
            apiResponse.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPatientHistory_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/patients/{nonExistentId}/history");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        #endregion

        #region Helper Methods

        private async Task<PatientDto> CreateTestPatientAsync(string name = "测试患者", string phoneNumber = "13800138000")
        {
            var createPatientDto = new PatientCreateDto
            {
                Name = name,
                PhoneNumber = phoneNumber,
                Gender = Gender.Male,
                DateOfBirth = new DateTime(1985, 5, 15),
                Address = "北京市海淀区",
                MedicalHistory = "无特殊病史"
            };

            var content = CreateJsonContent(createPatientDto);
            var response = await Client.PostAsync("/api/patients", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PatientDto>>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return apiResponse!.Data;
        }

        private StringContent CreateJsonContent<T>(T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        #endregion
    }
}