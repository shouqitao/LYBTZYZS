using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces {

    /// <summary>
    /// 处方API接口定义
    /// </summary>
    public interface IPrescriptionApi {

        Task<List<PrescriptionDto>> GetAllPrescriptionsAsync();

        Task<PrescriptionDetailDto?> GetPrescriptionByIdAsync(string id);

        Task<bool> CreatePrescriptionAsync(PrescriptionCreateDto prescription);

        Task<bool> UpdatePrescriptionAsync(PrescriptionEditDto prescription);

        Task<bool> DeletePrescriptionAsync(string id);

        Task<bool> CancelPrescriptionAsync(string id);

        // 可以扩展更多接口，如状态流转
    }
}