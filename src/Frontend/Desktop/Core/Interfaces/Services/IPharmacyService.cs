using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Pharmacy;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 药房服务接口
    /// </summary>
    public interface IPharmacyService
    {
        /// <summary>
        /// 获取今日处方列表
        /// </summary>
        Task<List<PrescriptionInfo>> GetTodayPrescriptionsAsync();

        /// <summary>
        /// 搜索处方
        /// </summary>
        Task<List<PrescriptionInfo>> SearchPrescriptionsAsync(PrescriptionSearchDto searchDto);

        /// <summary>
        /// 获取库存列表
        /// </summary>
        Task<List<StockInfo>> GetStockListAsync();


        /// <summary>
        /// 开始配药
        /// </summary>
        Task<bool> StartDispensingAsync(Guid prescriptionId);

        /// <summary>
        /// 完成配药
        /// </summary>
        Task<bool> CompleteDispensingAsync(Guid prescriptionId);

        /// <summary>
        /// 发药
        /// </summary>
        Task<bool> DispenseDrugAsync(Guid prescriptionId);

        /// <summary>
        /// 入库
        /// </summary>
        Task<bool> StockInAsync(Guid herbId, decimal quantity, string reason);

        /// <summary>
        /// 出库
        /// </summary>
        Task<bool> StockOutAsync(Guid herbId, decimal quantity, string reason);

        /// <summary>
        /// 库存盘点
        /// </summary>
        Task<bool> InventoryAsync(List<StockInfo> inventoryData);
    }
}