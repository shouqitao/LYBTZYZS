using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Services.Features
{
    /// <summary>
    /// 澶勬柟澶嶅埗鍜屾ā鏉挎湇鍔℃帴鍙?
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬澶勬柟澶嶅埗銆佸巻鍙插鏂瑰紩鐢ㄥ拰妯℃澘鍒涘缓鍔熻兘
    /// </summary>
    public interface IPrescriptionCopyService
    {
        /// <summary>
        /// 澶嶅埗澶勬柟
        /// </summary>
        /// <param name="originalId">鍘熷鏂笽D</param>        /// <param name="newName">鏂板鏂瑰悕绉?/param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>澶嶅埗鐨勫鏂笵TO</returns>
        Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid originalId, string newName, Guid operatorId, string operatorName);

        /// <summary>
        /// 澶嶅埗鎮ｈ€呯殑鏈€鍚庝竴娆″鏂?
        /// </summary>        /// <param name="patientId">鎮ｈ€匢D</param>        /// <param name="doctorId">鍖荤敓ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>澶嶅埗鐨勫鏂笵TO</returns>
        Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName);

        /// <summary>
        /// 浠庨獙鏂规ā鏉垮垱寤哄鏂?
        /// </summary>        /// <param name="templateId">楠屾柟妯℃澘ID</param>        /// <param name="patientId">鎮ｈ€匢D</param>        /// <param name="doctorId">鍖荤敓ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>
        /// <returns>鍒涘缓鐨勫鏂笵TO</returns>
        Task<ServiceResult<PrescriptionDto>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName);
    }
}

