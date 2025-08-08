using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Herbs;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 处方管理器接口
    /// </summary>
    public interface IPrescriptionManager
    {
        /// <summary>
        /// 当前处方项目集合
        /// </summary>
        ObservableCollection<PrescriptionItemInfo> PrescriptionItems { get; }

        /// <summary>
        /// 当前处方
        /// </summary>
        PrescriptionInfo? CurrentPrescription { get; set; }

        /// <summary>
        /// 处方总价
        /// </summary>
        decimal TotalPrice { get; }

        /// <summary>
        /// 添加药材到处方
        /// </summary>
        bool AddHerbToPrescription(HerbInfo herb, decimal quantity = 10m);

        /// <summary>
        /// 从处方中移除药材
        /// </summary>
        bool RemoveHerbFromPrescription(Guid herbId);

        /// <summary>
        /// 更新处方项目数量
        /// </summary>
        bool UpdateHerbQuantity(Guid herbId, decimal newQuantity);

        /// <summary>
        /// 清空处方
        /// </summary>
        void ClearPrescription();

        /// <summary>
        /// 保存处方
        /// </summary>
        Task<bool> SavePrescriptionAsync(Guid consultationId, string diagnosis, string dosageForm, int quantity, string usage);

        /// <summary>
        /// 验证整个处方
        /// </summary>
        Task<bool> ValidatePrescriptionAsync();

        /// <summary>
        /// 导入处方项目列表
        /// </summary>
        void ImportPrescriptionItems(IEnumerable<PrescriptionItemInfo> items);
    }
}