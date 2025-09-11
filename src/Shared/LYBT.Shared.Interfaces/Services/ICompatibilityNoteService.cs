using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Compatibility;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 配伍记录服务接口 - Record-Only模式已移除配伍检查功能
    /// </summary>
    [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
    public interface ICompatibilityNoteService
    {
        /// <summary>
        /// 创建配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="createDto">创建数据</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>创建的配伍记录</returns>
        Task<ServiceResult<CompatibilityNoteDto>> CreateAsync(
            Guid prescriptionId,
            CompatibilityNoteCreateDto createDto,
            Guid currentUserId);

        /// <summary>
        /// 根据处方ID获取配伍记录列表
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>配伍记录列表</returns>
        Task<ServiceResult<List<CompatibilityNoteDto>>> GetByPrescriptionIdAsync(Guid prescriptionId);

        /// <summary>
        /// 更新配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>更新后的配伍记录</returns>
        Task<ServiceResult<CompatibilityNoteDto>> UpdateAsync(
            Guid prescriptionId,
            Guid noteId,
            CompatibilityNoteUpdateDto updateDto,
            Guid currentUserId);

        /// <summary>
        /// 删除配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId, Guid noteId, Guid currentUserId);

        /// <summary>
        /// 根据ID获取单个配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <returns>配伍记录详情</returns>
        Task<ServiceResult<CompatibilityNoteDto>> GetByIdAsync(Guid prescriptionId, Guid noteId);
    }
}
