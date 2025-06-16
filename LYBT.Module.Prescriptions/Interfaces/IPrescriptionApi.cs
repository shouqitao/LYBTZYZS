using LYBT.Module.Prescriptions.Models;

namespace LYBT.Module.Prescriptions.Interfaces {

    /// <summary>
    /// 处方API接口定义
    /// </summary>
    public interface IPrescriptionApi {

        Task<List<PrescriptionModel>> GetAllPrescriptionsAsync();

        Task<PrescriptionModel> GetPrescriptionByIdAsync(string id);

        Task<bool> CreatePrescriptionAsync(PrescriptionModel prescription);

        Task<bool> UpdatePrescriptionAsync(PrescriptionModel prescription);

        Task<bool> DeletePrescriptionAsync(string id);

        // 可以扩展更多接口，如状态流转
    }
}