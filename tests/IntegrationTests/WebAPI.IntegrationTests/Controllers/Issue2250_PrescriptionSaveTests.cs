using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Tests.Common;
using LYBT.Tests.Common.AssertionHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// Issue #2250: 处方保存失败集成测试
    /// 验证RowVersion修复后处方创建/更新功能正常
    /// </summary>
    [Collection("Sequential")] // 串行执行避免测试干扰
    public class Issue2250_PrescriptionSaveTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testHerbId;

        private static readonly Guid FixedDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Issue2250_PrescriptionSaveTests(ITestOutputHelper output) : base()
        {
            _output = output;
            SetAuthorizationHeader(Client);
        }

        protected override string GenerateTestToken()
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes("TestSecretKey_MinLength32Characters_ForJWTTokenGeneration_123456789");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, FixedDoctorId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test Doctor"),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Doctor")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "LYBT.WebAPI.Tests",
                Audience = "LYBT.Client.Tests",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试医生
            var testDoctor = new LYBT.Entities.Users.User
            {
                Id = FixedDoctorId,
                UserName = "testdoctor",
                RealName = "测试医生",
                Email = "testdoctor@test.com",
                PasswordHash = "DummyHashForTesting",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Set<LYBT.Entities.Users.User>().Add(testDoctor);

            // 创建测试药材
            _testHerbId = Guid.NewGuid();
            var testHerb = new LYBT.Entities.Herbs.Herb
            {
                Id = _testHerbId,
                Name = "甘草",
                Category = "补气药",
                Price = 0.5m,
                Unit = "克",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            context.Set<LYBT.Entities.Herbs.Herb>().Add(testHerb);

            context.SaveChanges();
        }

        #region Issue #2250 核心测试

        /// <summary>
        /// Issue #2250 Test 1: 验证空Items被正确拒绝（负面测试）
        /// 业务规则: 处方必须包含至少一项药材
        /// </summary>
        [Fact]
        public async Task Issue2250_CreatePrescription_ShouldRejectEmptyItems()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();
            _output.WriteLine($"医案创建成功: {medicalCase.Id}");

            var request = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>() // 空Items
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                request);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"HTTP状态码: {response.StatusCode}");
            _output.WriteLine($"响应内容: {responseContent}");

            // 期望验证失败（BadRequest 400）
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest,
                "空Items应该触发FluentValidation验证失败");
            // 验证响应包含Items相关错误（JSON使用Unicode编码，检查"Items"关键字）
            responseContent.Should().Contain("Items");
            _output.WriteLine("验证通过: 空Items被正确拒绝");
        }

        /// <summary>
        /// Issue #2250 Test 2: 验证正常创建处方（核心正面测试）
        /// 目标: 确认RowVersion修复后处方能正常创建
        /// </summary>
        [Fact]
        public async Task Issue2250_CreatePrescription_ShouldSucceed_WithValidItems()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();
            _output.WriteLine($"医案创建成功: {medicalCase.Id}");

            var request = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Advice = "水煎服",
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = _testHerbId,
                        HerbName = "甘草",
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
                    }
                }
            };

            _output.WriteLine($"请求: MedicalCaseId={request.MedicalCaseId}, Items数量={request.Items.Count}");

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                request);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"HTTP状态码: {response.StatusCode}");
            _output.WriteLine($"响应内容: {responseContent}");

            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.MedicalCaseId.Should().Be(medicalCase.Id);

            _output.WriteLine($"处方创建成功: {apiResponse.Data.Id}");
        }

        /// <summary>
        /// Issue #2250 Test 3: 验证处方Items持久化
        /// 目标: 确认PrescriptionItem正确保存到数据库
        /// </summary>
        [Fact]
        public async Task Issue2250_CreatePrescription_ItemsShouldBePersisted()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            var request = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = _testHerbId,
                        HerbName = "甘草",
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
                    }
                }
            };

            // Act
            var createResponse = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                request);

            var createContent = await createResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"创建响应: {createResponse.StatusCode}");

            if (!createResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"创建失败: {createContent}");
                createResponse.ShouldBeOk();
                return;
            }

            var createResult = await createResponse.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            var prescriptionId = createResult.Data!.Id;
            _output.WriteLine($"处方创建成功: {prescriptionId}");

            // Assert - 从数据库验证Items
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            var prescriptionItems = db.Set<LYBT.Entities.Prescriptions.PrescriptionItem>()
                .Where(pi => pi.PrescriptionId == prescriptionId)
                .ToList();

            _output.WriteLine($"数据库中Items数量: {prescriptionItems.Count}");

            prescriptionItems.Should().HaveCount(1, "应该有1个药材项");
            prescriptionItems[0].HerbId.Should().Be(_testHerbId);
            prescriptionItems[0].Dosage.Should().Be(10);

            _output.WriteLine("Items持久化验证通过");
        }

        /// <summary>
        /// Issue #2250 Test 4: 验证处方更新功能
        /// 目标: 确认创建后可正常更新（无RowVersion冲突）
        /// </summary>
        [Fact]
        public async Task Issue2250_UpdatePrescription_ShouldSucceed_AfterCreate()
        {
            // Arrange - 创建医案和处方
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();

            var createRequest = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = _testHerbId,
                        HerbName = "甘草",
                        Dosage = 10,
                        Unit = "g",
                        DecocteMethod = DecocteMethod.Default,
                        UnitPrice = 0.5m
                    }
                }
            };

            var createResponse = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                createRequest);

            var createContent = await createResponse.Content.ReadAsStringAsync();
            if (!createResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"创建失败: {createContent}");
                createResponse.ShouldBeOk();
                return;
            }

            var createResult = await createResponse.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            var prescriptionId = createResult.Data!.Id;
            _output.WriteLine($"初始处方创建成功: {prescriptionId}");

            // Act - 更新处方
            var updateRequest = new PrescriptionInputDto
            {
                Id = prescriptionId,
                MedicalCaseId = medicalCase.Id,
                DosageCount = 14,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
                    {
                        HerbId = _testHerbId,
                        HerbName = "甘草",
                        Dosage = 15
                    }
                }
            };

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions/{prescriptionId}",
                updateRequest);

            var updateContent = await updateResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"更新响应: {updateResponse.StatusCode}");

            if (!updateResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"更新失败: {updateContent}");
            }

            // Assert
            updateResponse.ShouldBeOk();
            _output.WriteLine("处方更新成功");

            // 验证更新后的数据
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            var updatedPrescription = db.Set<LYBT.Entities.Prescriptions.Prescription>()
                .FirstOrDefault(p => p.Id == prescriptionId);

            updatedPrescription.Should().NotBeNull();
            updatedPrescription!.DosageCount.Should().Be(14);

            var updatedItems = db.Set<LYBT.Entities.Prescriptions.PrescriptionItem>()
                .Where(pi => pi.PrescriptionId == prescriptionId)
                .ToList();

            updatedItems.Should().HaveCount(1);
            updatedItems[0].Dosage.Should().Be(15);

            _output.WriteLine("更新后Items验证通过");
        }

        /// <summary>
        /// Issue #2250 Test 5: 验证连续更新不触发并发异常
        /// 目标: 确认RowVersion修复后连续操作正常
        /// </summary>
        [Fact]
        public async Task Issue2250_ConsecutiveUpdates_ShouldNotCauseConcurrencyException()
        {
            // Arrange
            var medicalCase = await CreateTestMedicalCaseReadyForPrescriptionAsync();
            _output.WriteLine($"医案创建成功: {medicalCase.Id}");

            // Step 1: 创建处方
            var createRequest = new PrescriptionInputDto
            {
                MedicalCaseId = medicalCase.Id,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto { HerbId = _testHerbId, HerbName = "甘草", Dosage = 10, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
                }
            };

            var createResponse = await Client.PostAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions",
                createRequest);

            var createContent = await createResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Step1 创建响应: {createResponse.StatusCode}");

            if (!createResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"Step1 创建失败: {createContent}");
                createResponse.ShouldBeOk();
                return;
            }

            var createResult = await createResponse.ShouldBeSuccessfulApiResponseAsync<PrescriptionEntity>();
            var prescriptionId = createResult.Data!.Id;
            _output.WriteLine($"Step1 处方创建成功: {prescriptionId}");

            // Step 2: 第一次更新
            var updateRequest1 = new PrescriptionInputDto
            {
                Id = prescriptionId,
                MedicalCaseId = medicalCase.Id,
                DosageCount = 14,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto { HerbId = _testHerbId, HerbName = "甘草", Dosage = 15, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
                }
            };

            var updateResponse1 = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions/{prescriptionId}",
                updateRequest1);

            var updateContent1 = await updateResponse1.Content.ReadAsStringAsync();
            _output.WriteLine($"Step2 更新响应: {updateResponse1.StatusCode}");

            if (!updateResponse1.IsSuccessStatusCode)
            {
                _output.WriteLine($"Step2 更新失败: {updateContent1}");
            }

            updateResponse1.ShouldBeOk();
            _output.WriteLine("Step2 更新成功");

            // Step 3: 第二次更新（连续操作）
            var updateRequest2 = new PrescriptionInputDto
            {
                Id = prescriptionId,
                MedicalCaseId = medicalCase.Id,
                DosageCount = 21,
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto { HerbId = _testHerbId, HerbName = "甘草", Dosage = 20, Unit = "g", DecocteMethod = DecocteMethod.Default, UnitPrice = 0.5m }
                }
            };

            var updateResponse2 = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescriptions/{prescriptionId}",
                updateRequest2);

            var updateContent2 = await updateResponse2.Content.ReadAsStringAsync();
            _output.WriteLine($"Step3 更新响应: {updateResponse2.StatusCode}");

            if (!updateResponse2.IsSuccessStatusCode)
            {
                _output.WriteLine($"Step3 更新失败: {updateContent2}");
            }

            updateResponse2.ShouldBeOk();
            _output.WriteLine("Step3 连续更新成功，无并发异常");
        }

        #endregion

        #region Helper Methods

        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseAsync()
        {
            var newPatientId = Guid.NewGuid();

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var testPatient = new LYBT.Entities.Patients.Patient
                {
                    Id = newPatientId,
                    Name = $"患者{newPatientId.ToString().Substring(0, 8)}",
                    Gender = Gender.Male,
                    PhoneNumber = "13800138000",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = FixedDoctorId,
                    UpdatedBy = FixedDoctorId
                };
                db.Set<LYBT.Entities.Patients.Patient>().Add(testPatient);
                await db.SaveChangesAsync();
            }

            var request = new { PatientId = newPatientId, VisitDate = DateTime.Now };
            var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"创建病案失败: {response.StatusCode} - {errorContent}");
            }

            response.ShouldBeOk();
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseWithConsultationAsync()
        {
            var medicalCase = await CreateTestMedicalCaseAsync();

            var consultationRequest = new ConsultationInputDto
            {
                PresentIllness = "头痛",
                TCMDiagnosis = "风寒感冒"
            };

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/consultation",
                consultationRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"更新辨证失败: {updateResponse.StatusCode} - {errorContent}");
            }

            updateResponse.ShouldBeOk();

            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        private async Task<MedicalCaseDetailDto> CreateTestMedicalCaseReadyForPrescriptionAsync()
        {
            var medicalCase = await CreateTestMedicalCaseWithConsultationAsync();

            var flagRequest = new { NeedsPrescription = true };
            var flagResponse = await Client.PutAsJsonAsync(
                $"/api/v1/medicalcases/{medicalCase.Id}/prescription-flag",
                flagRequest);

            if (!flagResponse.IsSuccessStatusCode)
            {
                var errorContent = await flagResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"设置处方标志失败: {flagResponse.StatusCode} - {errorContent}");
            }

            flagResponse.ShouldBeOk();

            // 诊断：检查设置标志后是否已存在处方
            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
                var existingPrescription = db.Set<LYBT.Entities.Prescriptions.Prescription>()
                    .FirstOrDefault(p => p.MedicalCaseId == medicalCase.Id);
                if (existingPrescription != null)
                {
                    _output.WriteLine($"[诊断] 设置标志后已存在处方: ID={existingPrescription.Id}, MedicalCaseId={existingPrescription.MedicalCaseId}");
                }
                else
                {
                    _output.WriteLine($"[诊断] 设置标志后无处方存在");
                }

                // 检查所有处方数量
                var allPrescriptions = db.Set<LYBT.Entities.Prescriptions.Prescription>().ToList();
                _output.WriteLine($"[诊断] 数据库中总处方数: {allPrescriptions.Count}");
                foreach (var p in allPrescriptions)
                {
                    _output.WriteLine($"[诊断] 处方: ID={p.Id}, MedicalCaseId={p.MedicalCaseId}");
                }
            }

            var getResponse = await Client.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<MedicalCaseDetailDto>();
            return apiResponse.Data!;
        }

        #endregion
    }
}
