using System;
using System.Collections.Generic;
using AutoMapper;
using Xunit;
using LYBT.Module.Patients.Mapping;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Tests.Mapping
{
    /// <summary>
    /// Patient模块DTO映射验证测试
    /// UltraThink质量保证：确保Patient相关的所有DTO映射正确无误
    /// </summary>
    public class PatientDtoMappingValidationTests : BaseDtoMappingValidationTests
    {
        protected override IEnumerable<Profile> GetMappingProfiles()
        {
            yield return new PatientMappingProfile();
        }

        protected override IEnumerable<(Type Source, Type Destination)> GetMappingPairs()
        {
            // Patient ↔ PatientDto 双向映射
            yield return (typeof(Patient), typeof(PatientDto));
            yield return (typeof(PatientDto), typeof(Patient));

            // PatientCreateDto → Patient 单向映射
            yield return (typeof(PatientCreateDto), typeof(Patient));

            // PatientUpdateDto → Patient 单向映射  
            yield return (typeof(PatientUpdateDto), typeof(Patient));
        }

        /// <summary>
        /// 测试Patient实体到DTO的映射
        /// </summary>
        [Fact]
        public void MapPatientToPatientDto_ShouldMapAllFields()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                PinYinCode = "ZS",
                Gender = Gender.Male,
                Age = 35,
                PhoneNumber = "13800138000",
                IdNumber = "110101198901010001",
                Address = "北京市东城区",
                EmergencyContact = "李四",
                EmergencyPhone = "13900139000",
                AllergyHistory = "青霉素过敏",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now.AddDays(-10),
                UpdateTime = DateTime.Now.AddHours(-1)
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(patient);

            // Assert - 验证所有重要字段都正确映射
            Assert.Equal(patient.Id, patientDto.Id);
            Assert.Equal(patient.Name, patientDto.Name);
            Assert.Equal(patient.PinYinCode, patientDto.PinYinCode);
            Assert.Equal(patient.Gender.ToString(), patientDto.Gender);
            Assert.Equal(patient.Age, patientDto.Age);
            Assert.Equal(patient.PhoneNumber, patientDto.PhoneNumber);
            Assert.Equal(patient.IdNumber, patientDto.IdNumber);
            Assert.Equal(patient.Address, patientDto.Address);
            Assert.Equal(patient.EmergencyContact, patientDto.EmergencyContact);
            Assert.Equal(patient.EmergencyPhone, patientDto.EmergencyPhone);
            Assert.Equal(patient.AllergyHistory, patientDto.AllergyHistory);
            Assert.Equal(patient.Status.ToString(), patientDto.Status);
            Assert.Equal(patient.CreateTime, patientDto.CreateTime);
            Assert.Equal(patient.UpdateTime, patientDto.UpdateTime);
        }

        /// <summary>
        /// 测试PatientCreateDto到Patient实体的映射
        /// </summary>
        [Fact]
        public void MapPatientCreateDtoToPatient_ShouldMapAllFields()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "王五",
                Gender = "Female",
                Age = 28,
                PhoneNumber = "13700137000",
                IdNumber = "110101199501010002",
                Address = "北京市西城区",
                EmergencyContact = "赵六",
                EmergencyPhone = "13600136000",
                AllergyHistory = "无已知过敏",
                Remark = "新患者档案"
            };

            // Act
            var patient = _mapper.Map<Patient>(createDto);

            // Assert - 验证创建映射的正确性
            Assert.Equal(createDto.Name, patient.Name);
            Assert.Equal(Gender.Female, patient.Gender);
            Assert.Equal(createDto.Age, patient.Age);
            Assert.Equal(createDto.PhoneNumber, patient.PhoneNumber);
            Assert.Equal(createDto.IdNumber, patient.IdNumber);
            Assert.Equal(createDto.Address, patient.Address);
            Assert.Equal(createDto.EmergencyContact, patient.EmergencyContact);
            Assert.Equal(createDto.EmergencyPhone, patient.EmergencyPhone);
            Assert.Equal(createDto.AllergyHistory, patient.AllergyHistory);
            Assert.Equal(createDto.Remark, patient.Remark);
        }

        /// <summary>
        /// 测试PatientUpdateDto到Patient实体的映射
        /// </summary>
        [Fact]
        public void MapPatientUpdateDtoToPatient_ShouldMapAllFields()
        {
            // Arrange
            var existingPatient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "原始姓名",
                Gender = Gender.Male,
                Age = 30,
                Status = CommonStatus.Enabled
            };

            var updateDto = new PatientUpdateDto
            {
                Id = existingPatient.Id,
                Name = "更新姓名",
                Gender = "Female",
                Age = 32,
                PhoneNumber = "13500135000",
                Address = "更新地址",
                AllergyHistory = "更新过敏史",
                Status = "Disabled",
                Remark = "更新备注"
            };

            // Act
            _mapper.Map(updateDto, existingPatient);

            // Assert - 验证更新映射的完整性
            Assert.Equal(updateDto.Id, existingPatient.Id);
            Assert.Equal(updateDto.Name, existingPatient.Name);
            Assert.Equal(Gender.Female, existingPatient.Gender);
            Assert.Equal(updateDto.Age, existingPatient.Age);
            Assert.Equal(updateDto.PhoneNumber, existingPatient.PhoneNumber);
            Assert.Equal(updateDto.Address, existingPatient.Address);
            Assert.Equal(updateDto.AllergyHistory, existingPatient.AllergyHistory);
            Assert.Equal(CommonStatus.Disabled, existingPatient.Status);
            Assert.Equal(updateDto.Remark, existingPatient.Remark);
        }

        /// <summary>
        /// 测试复杂Patient数据的往返映射一致性
        /// </summary>
        [Fact]
        public void PatientRoundTripMapping_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var originalPatient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "往返测试患者",
                PinYinCode = "WFCSHE",
                Gender = Gender.Female,
                Age = 45,
                PhoneNumber = "13400134000",
                IdNumber = "110101197901010003",
                Address = "测试地址123号",
                EmergencyContact = "紧急联系人",
                EmergencyPhone = "13300133000",
                AllergyHistory = "海鲜过敏，花粉过敏",
                Status = CommonStatus.Enabled,
                Remark = "重要患者",
                CreateTime = DateTime.Now.AddDays(-60),
                UpdateTime = DateTime.Now.AddDays(-1)
            };

            // Act - 往返映射：Patient → PatientDto → Patient
            var patientDto = _mapper.Map<PatientDto>(originalPatient);
            var roundTripPatient = _mapper.Map<Patient>(patientDto);

            // Assert - 验证关键数据保持一致
            Assert.Equal(originalPatient.Id, roundTripPatient.Id);
            Assert.Equal(originalPatient.Name, roundTripPatient.Name);
            Assert.Equal(originalPatient.Gender, roundTripPatient.Gender);
            Assert.Equal(originalPatient.Age, roundTripPatient.Age);
            Assert.Equal(originalPatient.PhoneNumber, roundTripPatient.PhoneNumber);
            Assert.Equal(originalPatient.IdNumber, roundTripPatient.IdNumber);
            Assert.Equal(originalPatient.Status, roundTripPatient.Status);
        }

        /// <summary>
        /// 测试边界条件和空值处理
        /// </summary>
        [Fact]
        public void PatientMapping_ShouldHandleNullAndEmptyValues()
        {
            // Arrange
            var patientWithNulls = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = Gender.Male,
                Age = 0,
                PhoneNumber = null,
                Address = "",
                EmergencyContact = null,
                AllergyHistory = null,
                Remark = null
            };

            // Act & Assert - 映射不应该抛出异常
            var exception = Record.Exception(() =>
            {
                var dto = _mapper.Map<PatientDto>(patientWithNulls);
                Assert.NotNull(dto);
                Assert.Equal(patientWithNulls.Name, dto.Name);
            });

            Assert.Null(exception);
        }
    }
}