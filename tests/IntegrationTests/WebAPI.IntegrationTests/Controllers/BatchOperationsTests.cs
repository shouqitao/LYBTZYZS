using FluentAssertions;
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace LYBT.IntegrationTests.Controllers
{
    /// <summary>
    /// 批量操作API集成测试
    /// OpenSpec: optimize-batch-operations Phase 2 - Task 2.8.2
    /// 覆盖: Users, Herbs, Formulas, Patients, MedicalCases 模块的批量删除/启用/禁用端点
    /// </summary>
    public class BatchOperationsTests : IntegrationTestBase
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BatchOperationsTests()
            : base()
        {
        }

        #region Users Batch Operations

        [Fact]
        public async Task Users_BatchDelete_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var userIds = await CreateTestUsersAsync(3);
            var request = new BatchDeleteInputDto { Ids = userIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TotalCount.Should().Be(3);
            apiResponse.Data.SuccessCount.Should().Be(3);
            apiResponse.Data.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task Users_BatchDelete_WithEmptyIds_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new BatchDeleteInputDto { Ids = new List<Guid>() };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Users_BatchDelete_WithNonExistentIds_ShouldReturnPartialSuccess()
        {
            // Arrange
            var existingUserIds = await CreateTestUsersAsync(2);
            var nonExistentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var allIds = existingUserIds.Concat(nonExistentIds).ToList();
            var request = new BatchDeleteInputDto { Ids = allIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(2);
            apiResponse.Data.FailureCount.Should().Be(2);
        }

        [Fact]
        public async Task Users_BatchEnable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var userIds = await CreateTestUsersAsync(3, CommonStatus.Disabled);
            var request = new BatchDeleteInputDto { Ids = userIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-enable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        [Fact]
        public async Task Users_BatchDisable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var userIds = await CreateTestUsersAsync(3, CommonStatus.Enabled);
            var request = new BatchDeleteInputDto { Ids = userIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-disable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        #endregion

        #region Herbs Batch Operations

        [Fact]
        public async Task Herbs_BatchDelete_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var herbIds = await CreateTestHerbsAsync(3);
            var request = new BatchDeleteInputDto { Ids = herbIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/herbs/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
            apiResponse.Data.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task Herbs_BatchEnable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var herbIds = await CreateTestHerbsAsync(3, CommonStatus.Disabled);
            var request = new BatchDeleteInputDto { Ids = herbIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/herbs/batch-enable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        [Fact]
        public async Task Herbs_BatchDisable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var herbIds = await CreateTestHerbsAsync(3, CommonStatus.Enabled);
            var request = new BatchDeleteInputDto { Ids = herbIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/herbs/batch-disable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        #endregion

        #region Formulas Batch Operations

        [Fact]
        public async Task Formulas_BatchDelete_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var formulaIds = await CreateTestFormulasAsync(3);
            var request = new BatchDeleteInputDto { Ids = formulaIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/formulas/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
            apiResponse.Data.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task Formulas_BatchEnable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var formulaIds = await CreateTestFormulasAsync(3, CommonStatus.Disabled);
            var request = new BatchDeleteInputDto { Ids = formulaIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/formulas/batch-enable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        [Fact]
        public async Task Formulas_BatchDisable_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var formulaIds = await CreateTestFormulasAsync(3, CommonStatus.Enabled);
            var request = new BatchDeleteInputDto { Ids = formulaIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/formulas/batch-disable", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
        }

        #endregion

        #region Patients Batch Operations

        [Fact]
        public async Task Patients_BatchDelete_WithValidIds_ShouldReturnSuccess()
        {
            // Arrange
            var patientIds = await CreateTestPatientsAsync(3);
            var request = new BatchDeleteInputDto { Ids = patientIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/patients/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.SuccessCount.Should().Be(3);
            apiResponse.Data.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task Patients_BatchDelete_WithEmptyIds_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new BatchDeleteInputDto { Ids = new List<Guid>() };

            // Act
            var response = await Client.PostAsJsonAsync("/api/patients/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region MedicalCases Batch Operations

        [Fact]
        public async Task MedicalCases_BatchDelete_WithEmptyIds_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new BatchDeleteInputDto { Ids = new List<Guid>() };

            // Act
            var response = await Client.PostAsJsonAsync("/api/medicalcases/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task MedicalCases_BatchDelete_WithNonExistentIds_ShouldReturnPartialResult()
        {
            // Arrange
            var nonExistentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var request = new BatchDeleteInputDto { Ids = nonExistentIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/medicalcases/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.FailureCount.Should().Be(2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task BatchOperation_WithDuplicateIds_ShouldDeduplicateAndProcess()
        {
            // Arrange
            var userIds = await CreateTestUsersAsync(2);
            var duplicatedIds = userIds.Concat(userIds).ToList(); // 重复ID
            var request = new BatchDeleteInputDto { Ids = duplicatedIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            // 服务端应该去重处理
            apiResponse.Data!.SuccessCount.Should().BeLessThanOrEqualTo(2);
        }

        [Fact]
        public async Task BatchOperation_WithLargeNumberOfIds_ShouldProcessSuccessfully()
        {
            // Arrange - 创建10个测试用户
            var userIds = await CreateTestUsersAsync(10);
            var request = new BatchDeleteInputDto { Ids = userIds };

            // Act
            var response = await Client.PostAsJsonAsync("/api/users/batch-delete", request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<BatchOperationResultDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TotalCount.Should().Be(10);
            apiResponse.Data.SuccessCount.Should().Be(10);
        }

        #endregion

        #region Helper Methods

        private async Task<List<Guid>> CreateTestUsersAsync(int count, CommonStatus status = CommonStatus.Enabled)
        {
            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var createDto = new UserInputDto
                {
                    UserName = $"batchtest_user_{Guid.NewGuid():N}",
                    RealName = $"批量测试用户{i + 1}",
                    Email = $"batch{i}_{Guid.NewGuid():N}@test.com",
                    Role = UserRole.Doctor
                };

                var response = await Client.PostAsJsonAsync("/api/users", createDto);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDetailDto>>(content, _jsonOptions);
                    if (apiResponse?.Data?.Id != null && apiResponse.Data.Id != Guid.Empty)
                    {
                        ids.Add(apiResponse.Data.Id);

                        // 如果需要禁用状态，调用batch-disable端点
                        if (status == CommonStatus.Disabled)
                        {
                            await Client.PostAsJsonAsync("/api/users/batch-disable",
                                new BatchDeleteInputDto { Ids = new List<Guid> { apiResponse.Data.Id } });
                        }
                    }
                }
            }
            return ids;
        }

        private async Task<List<Guid>> CreateTestHerbsAsync(int count, CommonStatus status = CommonStatus.Enabled)
        {
            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var createDto = new HerbInputDto
                {
                    Name = $"批量测试药材{i + 1}_{Guid.NewGuid():N}",
                    PinYinCode = $"piliang{i}",
                    Category = "测试类"
                };

                var response = await Client.PostAsJsonAsync("/api/herbs", createDto);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<LYBT.Shared.Models.Contracts.Common.ApiResponse<HerbDetailDto>>(content, _jsonOptions);
                    if (apiResponse?.Data?.Id != null && apiResponse.Data.Id != Guid.Empty)
                    {
                        ids.Add(apiResponse.Data.Id);

                        // 如果需要禁用状态，调用batch-disable端点
                        if (status == CommonStatus.Disabled)
                        {
                            await Client.PostAsJsonAsync("/api/herbs/batch-disable",
                                new BatchDeleteInputDto { Ids = new List<Guid> { apiResponse.Data.Id } });
                        }
                    }
                }
            }
            return ids;
        }

        private async Task<List<Guid>> CreateTestFormulasAsync(int count, CommonStatus status = CommonStatus.Enabled)
        {
            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var createDto = new FormulaInputDto
                {
                    Name = $"批量测试验方{i + 1}_{Guid.NewGuid():N}",
                    Category = "测试类"
                };

                var response = await Client.PostAsJsonAsync("/api/formulas", createDto);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDetailDto>>(content, _jsonOptions);
                    if (apiResponse?.Data?.Id != null && apiResponse.Data.Id != Guid.Empty)
                    {
                        ids.Add(apiResponse.Data.Id);

                        // 如果需要禁用状态，调用batch-disable端点
                        if (status == CommonStatus.Disabled)
                        {
                            await Client.PostAsJsonAsync("/api/formulas/batch-disable",
                                new BatchDeleteInputDto { Ids = new List<Guid> { apiResponse.Data.Id } });
                        }
                    }
                }
            }
            return ids;
        }

        private async Task<List<Guid>> CreateTestPatientsAsync(int count)
        {
            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var createDto = new PatientInputDto
                {
                    Name = $"批量测试患者{i + 1}",
                    PhoneNumber = $"138{i:D8}",
                    Gender = Gender.Male,
                    BirthDate = DateTime.Now.AddYears(-30)
                };

                var response = await Client.PostAsJsonAsync("/api/patients", createDto);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDetailDto>>(content, _jsonOptions);
                    if (apiResponse?.Data?.Id != null && apiResponse.Data.Id != Guid.Empty)
                    {
                        ids.Add(apiResponse.Data.Id);
                    }
                }
            }
            return ids;
        }

        #endregion
    }
}
