using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Aggregates.PatientAggregate.Events;
using LYBT.Domain.Aggregates.PatientAggregate.ValueObjects;
using SharedGender = LYBT.Shared.Models.Enums.Gender;

namespace LYBT.Domain.Aggregates.PatientAggregate
{
    /// <summary>
    /// 患者聚合根 - UltraThink重构DDD架构
    /// 管理患者的生命周期、医疗历史和诊疗状态
    /// </summary>
    public class Patient : AggregateRoot
    {
        /// <summary>
        /// 患者姓名
        /// </summary>
        public PatientName Name { get; private set; }

        /// <summary>
        /// 性别
        /// </summary>
        public Gender Gender { get; private set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateOfBirth DateOfBirth { get; private set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        public IdCardNumber IdCardNumber { get; private set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public ContactPhone ContactPhone { get; private set; }

        /// <summary>
        /// 紧急联系人
        /// </summary>
        public EmergencyContact EmergencyContact { get; private set; }

        /// <summary>
        /// 家庭住址
        /// </summary>
        public Address Address { get; private set; }

        /// <summary>
        /// 职业
        /// </summary>
        public string Occupation { get; private set; }

        /// <summary>
        /// 婚姻状况
        /// </summary>
        public MaritalStatus MaritalStatus { get; private set; }

        /// <summary>
        /// 过敏史
        /// </summary>
        public AllergyHistory AllergyHistory { get; private set; }

        /// <summary>
        /// 既往病史
        /// </summary>
        public MedicalHistory MedicalHistory { get; private set; }

        /// <summary>
        /// 家族病史
        /// </summary>
        public FamilyHistory FamilyHistory { get; private set; }

        /// <summary>
        /// 是否激活状态
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 首诊时间
        /// </summary>
        public DateTime? FirstVisitAt { get; private set; }

        /// <summary>
        /// 最后就诊时间
        /// </summary>
        public DateTime? LastVisitAt { get; private set; }

        /// <summary>
        /// 总就诊次数
        /// </summary>
        public int TotalVisits { get; private set; }

        /// <summary>
        /// 患者档案号（自动生成）
        /// </summary>
        public string FileNumber { get; private set; }

        // Repository兼容性属性
        /// <summary>
        /// 电话号码 - Repository兼容性属性
        /// </summary>
        public string PhoneNumber => ContactPhone?.Value ?? "";

        /// <summary>
        /// 身份信息 - Repository兼容性属性
        /// </summary>
        public PatientIdentity Identity => new PatientIdentity(IdCardNumber?.Value ?? "", "身份证");

        /// <summary>
        /// 个人信息 - Repository兼容性属性
        /// </summary>
        public PatientPersonalInfo PersonalInfo => new PatientPersonalInfo(
            Name?.PinYinCode ?? "",
            Name?.WuBiCode ?? "",
            Occupation ?? "",
            MaritalStatus?.Name ?? "");

        /// <summary>
        public string PatientNumber { get; private set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remarks { get; private set; }

        // EF Core需要的无参构造函数
        private Patient() { }

        /// <summary>
        /// 创建新患者
        /// </summary>
        /// <param name="name">患者姓名</param>
        /// <param name="gender">性别</param>
        /// <param name="dateOfBirth">出生日期</param>
        /// <param name="contactPhone">联系电话</param>
        /// <param name="createdBy">创建人</param>
        /// <param name="idCardNumber">身份证号（可选）</param>
        /// <param name="address">住址（可选）</param>
        /// <param name="occupation">职业（可选）</param>
        public static Patient Create(
            string name,
            Gender gender,
            DateTime? dateOfBirth,
            string contactPhone,
            Guid createdBy,
            string idCardNumber = null,
            string address = null,
            string occupation = null)
        {
            var patient = new Patient
            {
                Name = PatientName.Create(name),
                Gender = gender ?? throw new ArgumentNullException(nameof(gender)),
                DateOfBirth = dateOfBirth.HasValue ? DateOfBirth.Create(dateOfBirth.Value) : null,
                ContactPhone = ContactPhone.Create(contactPhone),
                IdCardNumber = !string.IsNullOrWhiteSpace(idCardNumber) ? IdCardNumber.Create(idCardNumber) : null,
                Address = !string.IsNullOrWhiteSpace(address) ? Address.Create(address) : null,
                Occupation = occupation?.Trim(),
                MaritalStatus = MaritalStatus.Unknown, // 默认未知
                IsActive = true,
                TotalVisits = 0
            };

            // 生成患者档案号
            patient.PatientNumber = GeneratePatientNumber();
            patient.SetCreationInfo(createdBy);

            // 发布患者创建领域事件
            patient.AddDomainEvent(new PatientCreatedEvent(
                patient.Id,
                patient.PatientNumber,
                patient.Name.Value,
                patient.Gender.Name,
                patient.ContactPhone.Value,
                createdBy));

            return patient;
        }

        /// <summary>
        /// 更新患者基本信息
        /// </summary>
        public void UpdateBasicInfo(
            string name = null,
            Gender gender = null,
            DateTime? dateOfBirth = null,
            string contactPhone = null,
            string idCardNumber = null,
            string address = null,
            string occupation = null,
            MaritalStatus maritalStatus = null,
            Guid? updatedBy = null)
        {
            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(name) && name != Name?.Value)
            {
                Name = PatientName.Create(name);
                hasChanges = true;
            }

            if (gender != null && gender != Gender)
            {
                Gender = gender;
                hasChanges = true;
            }

            if (dateOfBirth.HasValue && (DateOfBirth?.Value != dateOfBirth.Value))
            {
                DateOfBirth = DateOfBirth.Create(dateOfBirth.Value);
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(contactPhone) && contactPhone != ContactPhone?.Value)
            {
                ContactPhone = ContactPhone.Create(contactPhone);
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(idCardNumber) && idCardNumber != IdCardNumber?.Value)
            {
                IdCardNumber = IdCardNumber.Create(idCardNumber);
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(address) && address != Address?.Value)
            {
                Address = Address.Create(address);
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(occupation) && occupation.Trim() != Occupation)
            {
                Occupation = occupation.Trim();
                hasChanges = true;
            }

            if (maritalStatus != null && maritalStatus != MaritalStatus)
            {
                MaritalStatus = maritalStatus;
                hasChanges = true;
            }

            if (hasChanges)
            {
                MarkAsUpdated(updatedBy);
                AddDomainEvent(new PatientBasicInfoUpdatedEvent(Id, PatientNumber, Name.Value, updatedBy));
            }
        }

        /// <summary>
        /// 设置紧急联系人
        /// </summary>
        public void SetEmergencyContact(string name, string phone, string relationship, Guid? updatedBy = null)
        {
            EmergencyContact = EmergencyContact.Create(name, phone, relationship);
            MarkAsUpdated(updatedBy);
            
            AddDomainEvent(new PatientEmergencyContactUpdatedEvent(Id, PatientNumber, name, phone, relationship, updatedBy));
        }

        /// <summary>
        /// 更新过敏史
        /// </summary>
        public void UpdateAllergyHistory(List<string> allergies, Guid? updatedBy = null)
        {
            AllergyHistory = AllergyHistory.Create(allergies);
            MarkAsUpdated(updatedBy);

            AddDomainEvent(new PatientAllergyHistoryUpdatedEvent(Id, PatientNumber, allergies, updatedBy));
        }

        /// <summary>
        /// 更新既往病史
        /// </summary>
        public void UpdateMedicalHistory(List<string> medicalHistory, Guid? updatedBy = null)
        {
            MedicalHistory = MedicalHistory.Create(medicalHistory);
            MarkAsUpdated(updatedBy);

            AddDomainEvent(new PatientMedicalHistoryUpdatedEvent(Id, PatientNumber, medicalHistory, updatedBy));
        }

        /// <summary>
        /// 更新家族病史
        /// </summary>
        public void UpdateFamilyHistory(Dictionary<string, List<string>> familyHistory, Guid? updatedBy = null)
        {
            FamilyHistory = FamilyHistory.Create(familyHistory);
            MarkAsUpdated(updatedBy);

            AddDomainEvent(new PatientFamilyHistoryUpdatedEvent(Id, PatientNumber, familyHistory, updatedBy));
        }

        /// <summary>
        /// 记录就诊
        /// </summary>
        /// <param name="visitTime">就诊时间</param>
        /// <param name="doctorId">医生ID</param>
        public void RecordVisit(DateTime visitTime, Guid doctorId)
        {
            if (FirstVisitAt == null)
            {
                FirstVisitAt = visitTime;
                AddDomainEvent(new PatientFirstVisitRecordedEvent(Id, PatientNumber, visitTime, doctorId));
            }

            LastVisitAt = visitTime;
            TotalVisits++;

            AddDomainEvent(new PatientVisitRecordedEvent(Id, PatientNumber, visitTime, doctorId, TotalVisits));
        }

        /// <summary>
        /// 停用患者档案
        /// </summary>
        public void Deactivate(string reason, Guid deactivatedBy)
        {
            if (!IsActive) return;

            IsActive = false;
            MarkAsUpdated(deactivatedBy);

            AddDomainEvent(new PatientDeactivatedEvent(Id, PatientNumber, Name.Value, reason, deactivatedBy));
        }

        /// <summary>
        /// 激活患者档案
        /// </summary>
        public void Activate(Guid activatedBy)
        {
            if (IsActive) return;

            IsActive = true;
            MarkAsUpdated(activatedBy);

            AddDomainEvent(new PatientActivatedEvent(Id, PatientNumber, Name.Value, activatedBy));
        }

        /// <summary>
        /// 添加备注
        /// </summary>
        public void AddRemarks(string remarks, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(remarks)) return;

            Remarks = string.IsNullOrEmpty(Remarks) 
                ? remarks.Trim() 
                : $"{Remarks}\n{DateTime.Now:yyyy-MM-dd HH:mm}: {remarks.Trim()}";
            
            MarkAsUpdated(updatedBy);
        }

        /// <summary>
        /// 计算年龄
        /// </summary>
        public int? GetAge()
        {
            return DateOfBirth?.GetCurrentAge();
        }

        /// <summary>
        /// 检查是否为新患者（首次就诊）
        /// </summary>
        public bool IsNewPatient() => FirstVisitAt == null;

        /// <summary>
        /// 检查是否为老患者（有就诊历史）
        /// </summary>
        public bool IsReturningPatient() => FirstVisitAt != null;

        /// <summary>
        /// 检查是否有过敏史
        /// </summary>
        public bool HasAllergies() => AllergyHistory != null && AllergyHistory.HasAllergies();

        /// <summary>
        /// 获取完整的患者信息摘要
        /// </summary>
        public string GetSummary()
        {
            var age = GetAge();
            var ageInfo = age.HasValue ? $"{age}岁" : "年龄未知";
            var visitInfo = IsNewPatient() ? "新患者" : $"复诊患者(共{TotalVisits}次)";
            
            return $"{Name.Value} ({Gender.DisplayName}, {ageInfo}) - {visitInfo}";
        }

        /// <summary>
        /// 生成患者档案号
        /// </summary>
        private static string GeneratePatientNumber()
        {
            // 格式：P + 年份 + 月份 + 日期 + 时分秒 + 随机数
            var now = DateTime.Now;
            var random = new Random().Next(100, 999);
            return $"P{now:yyyyMMddHHmmss}{random}";
        }
    }
}