using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// 患者控制器集成测试
    /// 测试API端点的完整流程，包括数据库操作
    /// </summary>
    public class PatientsControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public PatientsControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        #region GET Tests

        [Fact]
        public async Task GetPatients_WithValidParameters_ShouldReturnPagedResults()
        {
            // Arrange - 在数据库中创建测试数据
            await SeedTestData();

            // Act
            var response = await Client.GetAsync("/api/patients?page=1&pageSize=10");

            // Assert
            response.Should().BeOk();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.TotalCount.Should().BeGreaterThan(0);
            apiResponse.Data.CurrentPage.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPatients_WithSearchKeyword_ShouldReturnFilteredResults()
        {
            // Arrange - 创建特定的测试数据
            await SeedTestData();
            
            // Act
            var response = await Client.GetAsync("/api/patients?page=1&pageSize=10&keyword=张");

            // Assert
            response.Should().BeOk();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().NotBeEmpty();
            apiResponse.Data.Items.All(p => p.Name.Contains("张")).Should().BeTrue();
        }

        [Fact]
        public async Task GetPatient_WithExistingId_ShouldReturnPatient()
        {
            // Arrange
            var createdPatient = await CreateTestPatientAsync();
            
            // Act
            var response = await Client.GetAsync($"/api/patients/{createdPatient.Id}");

            // Assert
            response.Should().BeOk();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(createdPatient.Id);
            apiResponse.Data.Name.Should().Be(createdPatient.Name);
        }

        [Fact]
        public async Task GetPatient_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act
            var response = await Client.GetAsync($"/api/patients/{nonExistingId}");

            // Assert
            response.ShouldBeNotFound();
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreatePatient_WithValidData_ShouldReturnCreatedPatient()
        {
            // Arrange
            var createDto = new PatientInputDto
            {
                Name = "集成测试患者",
                Gender = Gender.Male,
                BirthDate = new DateTime(1985, 5, 15),
                PhoneNumber = "13800138555",
                IdNumber = "110101198505151234",
                Address = "北京市海淀区测试街道123号",
                EmergencyContactName = "紧急联系人",
                EmergencyContactPhone = "13900139555"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/patients", createDto);

            // Assert
            response.ShouldBeCreated();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Name.Should().Be(createDto.Name);
            apiResponse.Data.Gender.Should().Be(createDto.Gender);
            apiResponse.Data.PhoneNumber.Should().Be(createDto.PhoneNumber);
            apiResponse.Data.Id.Should().NotBeEmpty();
            apiResponse.Data.IdNumber.Should().Be(createDto.IdNumber);
        }

        [Fact]
        public async Task CreatePatient_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - 缺少必需字段
            var invalidDto = new PatientInputDto
            {
                Name = "", // 空名称
                Gender = Gender.Male
                // 缺少其他必需字段
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/patients", invalidDto);

            // Assert
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreatePatient_WithDuplicateIdNumber_ShouldReturnBadRequest()
        {
            // Arrange - 先创建一个患者
            var existingPatient = await CreateTestPatientAsync();
            
            var duplicateDto = new PatientInputDto
            {
                Name = "重复身份证患者",
                Gender = Gender.Female,
                BirthDate = new DateTime(1992, 3, 20),
                PhoneNumber = "13800138666",
                IdNumber = existingPatient.IdNumber, // 重复的身份证号
                Address = "北京市朝阳区测试路456号"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/patients", duplicateDto);

            // Assert
            response.Should().BeBadRequest();
            
            var apiResponse = await response.ShouldBeFailedApiResponseWithMessageAsync();
            apiResponse.Message.Should().Contain("身份证号已存在");
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdatePatient_WithValidData_ShouldReturnUpdatedPatient()
        {
            // Arrange
            var createdPatient = await CreateTestPatientAsync();
            
            var updateDto = new PatientInputDto
            {
                Name = "更新后的患者姓名",
                PhoneNumber = "13800138777",
                Address = "更新后的地址：北京市西城区更新路789号",
                EmergencyContactName = "更新后的紧急联系人",
                EmergencyContactPhone = "13900139777"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"/api/patients/{createdPatient.Id}", updateDto);

            // Assert
            response.Should().BeOk();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(createdPatient.Id);
            apiResponse.Data.Name.Should().Be(updateDto.Name);
            apiResponse.Data.PhoneNumber.Should().Be(updateDto.PhoneNumber);
            apiResponse.Data.Address.Should().Be(updateDto.Address);
            // 确保其他字段没有被更改
            apiResponse.Data.Gender.Should().Be(createdPatient.Gender);
            apiResponse.Data.BirthDate.Should().Be(createdPatient.BirthDate);
        }

        [Fact]
        public async Task UpdatePatient_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updateDto = new PatientInputDto
            {
                Name = "不存在的患者更新"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"/api/patients/{nonExistingId}", updateDto);

            // Assert
            response.ShouldBeNotFound();
        }

        [Fact]
        public async Task UpdatePatient_WithDuplicateIdNumber_ShouldReturnBadRequest()
        {
            // Arrange - 创建两个患者
            var patient1 = await CreateTestPatientAsync();
            var patient2 = await CreateTestPatientAsync();
            
            var updateDto = new PatientInputDto
            {
                Name = "尝试重复身份证",
                IdNumber = patient2.IdNumber // 使用另一个患者的身份证号
            };

            // Act
            var response = await Client.PutAsJsonAsync($"/api/patients/{patient1.Id}", updateDto);

            // Assert
            response.Should().BeBadRequest();
            
            var apiResponse = await response.ShouldBeFailedApiResponseWithMessageAsync();
            apiResponse.Message.Should().Contain("身份证号已存在");
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeletePatient_WithExistingId_ShouldReturnNoContent()
        {
            // Arrange
            var createdPatient = await CreateTestPatientAsync();

            // Act
            var response = await Client.DeleteAsync($"/api/patients/{createdPatient.Id}");

            // Assert
            response.ShouldBeNoContent();

            // 验证患者确实被删除
            var getResponse = await Client.GetAsync($"/api/patients/{createdPatient.Id}");
            getResponse.ShouldBeNotFound();
        }

        [Fact]
        public async Task DeletePatient_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"/api/patients/{nonExistingId}");

            // Assert
            response.ShouldBeNotFound();
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchPatients_WithKeyword_ShouldReturnMatchingResults()
        {
            // Arrange - 创建多个患者，其中一些包含特定关键字
            await CreateTestPatientAsync("张三");
            await CreateTestPatientAsync("张小明");
            await CreateTestPatientAsync("李四");
            await CreateTestPatientAsync("张伟");

            // Act
            var response = await Client.GetAsync("/api/patients/search?keyword=张");

            // Assert
            response.Should().BeOk();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<List<PatientDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Should().HaveCount(3); // 张三、张小明、张伟
            apiResponse.Data.All(p => p.Name.Contains("张")).Should().BeTrue();
        }

        [Fact]
        public async Task SearchPatients_WithEmptyKeyword_ShouldReturnBadRequest()
        {
            // Act
            var response = await Client.GetAsync("/api/patients/search?keyword=");

            // Assert
            response.Should().BeBadRequest();
        }

        #endregion

        #region Helper Methods

        private async Task<PatientDto> CreateTestPatientAsync(string? name = null)
        {
            var createDto = new PatientInputDto
            {
                Name = name ?? $"测试患者_{Guid.NewGuid():N}",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = $"138{new Random().Next(10000000, 99999999)}",
                IdNumber = $"110101199001{new Random().Next(100000, 999999)}",
                Address = "测试地址",
                EmergencyContactName = "紧急联系人",
                EmergencyContactPhone = "13900000000"
            };

            var response = await Client.PostAsJsonAsync("/api/patients", createDto);
            response.Should().BeCreated();
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PatientDto>();
            return apiResponse.Data!;
        }

        private async Task SeedTestData()
        {
            // 创建一些基础测试数据
            var patients = new[]
            {
                new PatientInputDto
                {
                    Name = "张三",
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1980, 5, 15),
                    PhoneNumber = "13800138001",
                    IdNumber = "110101198005150001",
                    Address = "北京市朝阳区"
                },
                new PatientInputDto
                {
                    Name = "李四",
                    Gender = Gender.Female,
                    BirthDate = new DateTime(1985, 8, 20),
                    PhoneNumber = "13800138002",
                    IdNumber = "110101198508200002",
                    Address = "北京市海淀区"
                },
                new PatientInputDto
                {
                    Name = "王五",
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1990, 3, 10),
                    PhoneNumber = "13800138003",
                    IdNumber = "110101199003100003",
                    Address = "北京市西城区"
                }
            };

            foreach (var patient in patients)
            {
                await Client.PostAsJsonAsync("/api/patients", patient);
            }
        }

        #endregion
    }
}