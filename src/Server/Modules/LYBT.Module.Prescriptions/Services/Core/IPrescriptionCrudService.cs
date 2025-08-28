using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Services.Core
{
    /// <summary>
    /// 澶勬柟鍩虹CRUD鎿嶄綔鏈嶅姟鎺ュ彛
    /// UltraThink閲嶆瀯锛氬崟涓€鑱岃矗鍘熷垯锛屽彧璐熻矗鍩虹鐨勫鍒犳敼鏌ユ搷浣?
    /// </summary>
    public interface IPrescriptionCrudService
    {
        /// <summary>
        /// 鍒涘缓鏂板鏂?
        /// </summary>
        /// <param name="dto">鍒涘缓DTO</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鍒涘缓鐨勫鏂笵TO</returns>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 鏇存柊澶勬柟
        /// </summary>        /// <param name="dto">缂栬緫DTO</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 鍒犻櫎澶勬柟
        /// </summary>        /// <param name="id">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>
        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> DeleteAsync(Guid id, Guid operatorId, string operatorName);
    }
}

