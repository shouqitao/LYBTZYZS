using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Prescriptions.Services.Features
{
    /// <summary>
    /// 澶勬柟瀵煎嚭鏈嶅姟鎺ュ彛
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬澶勬柟鐨勫悇绉嶆牸寮忓鍑哄姛鑳?
    /// </summary>
    public interface IPrescriptionExportService
    {
        /// <summary>
        /// 瀵煎嚭澶勬柟涓篜DF
        /// </summary>
        /// <param name="prescriptionId">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>PDF鏂囦欢瀛楄妭鏁扮粍</returns>
        Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid prescriptionId, Guid operatorId, string operatorName);

        /// <summary>
        /// 瀵煎嚭澶勬柟涓篍xcel
        /// </summary>        /// <param name="prescriptionId">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>        /// <returns>Excel鏂囦欢瀛楄妭鏁扮粍</returns>
        Task<ServiceResult<byte[]>> ExportToExcelAsync(Guid prescriptionId, Guid operatorId, string operatorName);

        /// <summary>
        /// 瀵煎嚭澶勬柟涓烘枃鏈牸寮?
        /// </summary>        /// <param name="prescriptionId">澶勬柟ID</param>        /// <param name="operatorId">鎿嶄綔鑰匢D</param>        /// <param name="operatorName">鎿嶄綔鑰呭鍚?/param>
        /// <returns>鏂囨湰鍐呭</returns>
        Task<ServiceResult<string>> ExportToTextAsync(Guid prescriptionId, Guid operatorId, string operatorName);
    }
}

