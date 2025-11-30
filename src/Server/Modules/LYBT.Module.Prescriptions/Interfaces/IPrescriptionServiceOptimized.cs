using LYBT.Entities.Patients;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方服务优化接口 - 消除双重映射，直接返回Entity
    /// Phase 3 Task 3.2: Service层优化 - Entity直接返回策略
    /// 考虑复杂关联数据：MedicalCase、Patient、Consultation
    /// </summary>
    public interface IPrescriptionServiceOptimized
    {
        /// <summary>
        /// 根据ID获取处方（直接返回Prescription Entity，包含Items）
        /// </summary>
        Task<Result<Prescription>> GetByIdEntityAsync(Guid id);

        /// <summary>
        /// 根据医案ID获取处方列表（直接返回Prescription Entity列表，包含Items）
        /// </summary>
        Task<Result<List<Prescription>>> GetByMedicalCaseIdEntityAsync(Guid medicalCaseId);

        /// <summary>
        /// 搜索处方（直接返回Prescription Entity列表，包含预加载的关联数据）
        /// 返回结构：Prescription实体 + 预加载的关联数据（MedicalCase、Patient、Consultation）
        /// </summary>
        Task<Result<(List<Prescription> Prescriptions,
                   Dictionary<Guid, MedicalCaseEntity> MedicalCases,
                   Dictionary<Guid, Patient> Patients,
                   Dictionary<Guid, ConsultationEntity> Consultations)>> SearchPrescriptionsEntityAsync(
            string? patientName = null,
            string? symptomKeyword = null);

        /// <summary>
        /// 获取患者最近处方列表（直接返回Prescription Entity列表，包含预加载的关联数据）
        /// </summary>
        Task<Result<(List<Prescription> Prescriptions,
                   Dictionary<Guid, MedicalCaseEntity> MedicalCases,
                   Dictionary<Guid, Patient> Patients)>> GetPatientRecentPrescriptionsEntityAsync(
            Guid patientId, int count = 10);
    }
}