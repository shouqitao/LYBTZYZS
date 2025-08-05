using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Physiotherapy;
using LYBT.WPF.Client.Core.Models.TreatmentRoom;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 理疗服务接口
    /// </summary>
    public interface IPhysiotherapyService
    {
        /// <summary>
        /// 获取理疗执行记录列表
        /// </summary>
        Task<List<TreatmentExecutionInfo>> GetExecutionsAsync(DateTime? date = null, string? status = null);

        /// <summary>
        /// 获取理疗项目目录
        /// </summary>
        Task<List<TreatmentCatalogInfo>> GetTreatmentCatalogAsync();

        /// <summary>
        /// 创建理疗执行记录
        /// </summary>
        Task<bool> CreateExecutionAsync(TreatmentExecutionInfo execution);

        /// <summary>
        /// 更新执行记录状态
        /// </summary>
        Task<bool> UpdateExecutionStatusAsync(Guid executionId, string status);

        /// <summary>
        /// 开始理疗
        /// </summary>
        Task<bool> StartTreatmentAsync(Guid executionId);

        /// <summary>
        /// 完成理疗
        /// </summary>
        Task<bool> CompleteTreatmentAsync(Guid executionId, string notes);

        /// <summary>
        /// 取消理疗
        /// </summary>
        Task<bool> CancelExecutionAsync(Guid executionId, string reason);

        /// <summary>
        /// 新增理疗项目
        /// </summary>
        Task<bool> AddTreatmentCatalogAsync(TreatmentCatalogInfo catalog);

        /// <summary>
        /// 更新理疗项目
        /// </summary>
        Task<bool> UpdateTreatmentCatalogAsync(TreatmentCatalogInfo catalog);

        /// <summary>
        /// 删除理疗项目
        /// </summary>
        Task<bool> DeleteTreatmentCatalogAsync(Guid catalogId);

        /// <summary>
        /// 获取理疗师列表
        /// </summary>
        Task<List<UserInfo>> GetTherapistsAsync();

        /// <summary>
        /// 获取理疗预约列表
        /// </summary>
        Task<List<PhysiotherapyAppointmentInfo>> GetAppointmentsAsync(DateTime? date = null, string? status = null);

        /// <summary>
        /// 获取理疗项目类型列表
        /// </summary>
        Task<List<TreatmentTypeInfo>> GetTreatmentTypesAsync();
    }
}