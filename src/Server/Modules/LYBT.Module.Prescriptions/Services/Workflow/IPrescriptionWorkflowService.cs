using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Prescriptions.Services.Workflow
{
    /// <summary>
    /// 澶勬柟宸ヤ綔娴佺鐞嗘湇鍔℃帴鍙?
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬澶勬柟瀹℃壒娴佺▼鍜岀姸鎬佺鐞?
    /// </summary>
    public interface IPrescriptionWorkflowService
    {
        /// <summary>
        /// 鎵瑰噯澶勬柟
        /// </summary>
        /// <param name="id">澶勬柟ID</param>        /// <param name="approvalNote">鎵瑰噯澶囨敞</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote, Guid operatorId, string operatorName);

        /// <summary>
        /// 鎷掔粷澶勬柟
        /// </summary>        /// <param name="id">澶勬柟ID</param>        /// <param name="rejectionReason">鎷掔粷鍘熷洜</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> RejectAsync(Guid id, string rejectionReason, Guid operatorId, string operatorName);

        /// <summary>
        /// 鎻愪氦澶勬柟锛堢瓑寰呭鎵癸級
        /// </summary>        /// <param name="prescriptionId">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> SubmitAsync(Guid prescriptionId, Guid operatorId, string operatorName);

        /// <summary>
        /// 鍙栨秷澶勬柟
        /// </summary>        /// <param name="id">澶勬柟ID</param>        /// <param name="cancellationReason">鍙栨秷鍘熷洜</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> CancelAsync(Guid id, string cancellationReason, Guid operatorId, string operatorName);

        /// <summary>
        /// 蹇€熶繚瀛橈紙鑽夌鐘舵€侊級
        /// </summary>        /// <param name="id">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>
        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> QuickSaveAsync(Guid id, Guid operatorId, string operatorName);
    }
}

