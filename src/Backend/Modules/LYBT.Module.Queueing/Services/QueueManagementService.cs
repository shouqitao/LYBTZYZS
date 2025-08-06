using System.Threading.Tasks;
using System.Linq;
using System;
using LYBT.Infrastructure.Data;
using LYBT.Models.MedicalCase;
using LYBT.Models.Queueing;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Queueing.Services
{
    /// <summary>
    /// 队列管理服务实现 - 统一排队协调器
    /// </summary>
    public class QueueManagementService : IQueueManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QueueManagementService> _logger;

        public QueueManagementService(
            AppDbContext context,
            ILogger<QueueManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 添加到排队队列
        /// </summary>
        public async Task<QueueItemModel> AddToQueueAsync(Guid medicalCaseId, QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                // 获取医疗案例信息
                var medicalCase = await _context.MedicalCases
                    .Include(m => m.Registration)
                    .FirstOrDefaultAsync(m => m.Id == medicalCaseId);

                if (medicalCase == null)
                {
                    throw new InvalidOperationException("医疗案例不存在");
                }

                // 检查是否已在队列中
                var existingQueue = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.MedicalCaseId == medicalCaseId && 
                                            q.QueueType == queueType && 
                                            q.Status != QueueItemStatus.Completed &&
                                            q.Status != QueueItemStatus.Cancelled &&
                                            q.IsActive);

                if (existingQueue != null)
                {
                    return existingQueue;
                }

                // 获取患者姓名
                var patient = await _context.Patients.FindAsync(medicalCase.PatientId);
                var patientName = patient?.Name ?? "未知患者";

                // 获取服务点名称
                var servicePointName = await GetServicePointNameAsync(queueType, servicePointId);

                // 生成队列号
                var queueNumber = await GetNextQueueNumberAsync(queueType, servicePointId);

                // 创建排队项目
                var queueItem = new QueueItemModel
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCaseId,
                    PatientId = medicalCase.PatientId,
                    PatientName = patientName,
                    QueueType = queueType,
                    QueueNumber = queueNumber,
                    ServicePointId = servicePointId,
                    ServicePointName = servicePointName,
                    Status = QueueItemStatus.Waiting,
                    Priority = GetPriorityFromRegistration(medicalCase.Registration),
                    EstimatedWaitTime = await EstimateWaitTimeAsync(queueType, servicePointId),
                    CreateTime = DateTime.Now,
                    IsActive = true
                };

                _context.Set<QueueItemModel>().Add(queueItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("患者 {PatientName} 已加入 {QueueType} 队列，队列号：{QueueNumber}", 
                    patientName, queueType, queueNumber);

                return queueItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加到排队队列失败: MedicalCaseId={MedicalCaseId}, QueueType={QueueType}", 
                    medicalCaseId, queueType);
                throw;
            }
        }

        /// <summary>
        /// 获取指定类型的排队列表
        /// </summary>
        public async Task<List<QueueItemModel>> GetQueueByTypeAsync(QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                var query = _context.Set<QueueItemModel>()
                    .Where(q => q.QueueType == queueType && q.IsActive &&
                               (q.Status == QueueItemStatus.Waiting || 
                                q.Status == QueueItemStatus.Called ||
                                q.Status == QueueItemStatus.InService));

                if (servicePointId.HasValue)
                {
                    query = query.Where(q => q.ServicePointId == servicePointId.Value);
                }

                return await query
                    .OrderBy(q => q.Priority) // 优先级排序
                    .ThenBy(q => q.QueueTime)  // 时间排序
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取排队列表失败: QueueType={QueueType}", queueType);
                throw;
            }
        }

        /// <summary>
        /// 获取医生的看诊队列
        /// </summary>
        public async Task<List<QueueItemModel>> GetDoctorConsultationQueueAsync(Guid doctorId)
        {
            return await GetQueueByTypeAsync(QueueType.Consultation, doctorId);
        }

        /// <summary>
        /// 获取收费台队列
        /// </summary>
        public async Task<List<QueueItemModel>> GetPaymentQueueAsync()
        {
            return await GetQueueByTypeAsync(QueueType.Payment);
        }

        /// <summary>
        /// 获取药房队列
        /// </summary>
        public async Task<List<QueueItemModel>> GetPharmacyQueueAsync(Guid? pharmacyId = null)
        {
            return await GetQueueByTypeAsync(QueueType.Pharmacy, pharmacyId);
        }

        /// <summary>
        /// 获取理疗室队列
        /// </summary>
        public async Task<List<QueueItemModel>> GetTreatmentRoomQueueAsync(Guid? roomId = null)
        {
            return await GetQueueByTypeAsync(QueueType.TreatmentRoom, roomId);
        }

        /// <summary>
        /// 叫号
        /// </summary>
        public async Task<bool> CallNextAsync(QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                var nextItem = await GetNextWaitingItemAsync(queueType, servicePointId);
                
                if (nextItem == null)
                {
                    return false;
                }

                return await CallSpecificAsync(nextItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "叫号失败: QueueType={QueueType}", queueType);
                throw;
            }
        }

        /// <summary>
        /// 叫指定号码
        /// </summary>
        public async Task<bool> CallSpecificAsync(Guid queueItemId)
        {
            try
            {
                var queueItem = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.Id == queueItemId && q.IsActive);

                if (queueItem == null || queueItem.Status != QueueItemStatus.Waiting)
                {
                    return false;
                }

                queueItem.Status = QueueItemStatus.Called;
                queueItem.CallTime = DateTime.Now;
                queueItem.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("叫号成功: 患者 {PatientName}, 队列号 {QueueNumber}", 
                    queueItem.PatientName, queueItem.QueueNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "叫指定号码失败: QueueItemId={QueueItemId}", queueItemId);
                throw;
            }
        }

        /// <summary>
        /// 开始服务
        /// </summary>
        public async Task<bool> StartServiceAsync(Guid queueItemId)
        {
            try
            {
                var queueItem = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.Id == queueItemId && q.IsActive);

                if (queueItem == null || 
                    (queueItem.Status != QueueItemStatus.Waiting && queueItem.Status != QueueItemStatus.Called))
                {
                    return false;
                }

                queueItem.Status = QueueItemStatus.InService;
                queueItem.StartServiceTime = DateTime.Now;
                queueItem.UpdateTime = DateTime.Now;

                if (!queueItem.CallTime.HasValue)
                {
                    queueItem.CallTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("开始服务: 患者 {PatientName}, 队列号 {QueueNumber}", 
                    queueItem.PatientName, queueItem.QueueNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始服务失败: QueueItemId={QueueItemId}", queueItemId);
                throw;
            }
        }

        /// <summary>
        /// 完成服务
        /// </summary>
        public async Task<bool> CompleteServiceAsync(Guid queueItemId)
        {
            try
            {
                var queueItem = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.Id == queueItemId && q.IsActive);

                if (queueItem == null || queueItem.Status != QueueItemStatus.InService)
                {
                    return false;
                }

                queueItem.Status = QueueItemStatus.Completed;
                queueItem.CompleteServiceTime = DateTime.Now;
                queueItem.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("完成服务: 患者 {PatientName}, 队列号 {QueueNumber}", 
                    queueItem.PatientName, queueItem.QueueNumber);

                // 自动进入下一个队列（如果有的话）
                await AutoEnqueueNextStageAsync(queueItem);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成服务失败: QueueItemId={QueueItemId}", queueItemId);
                throw;
            }
        }

        /// <summary>
        /// 跳过当前号码
        /// </summary>
        public async Task<bool> SkipAsync(Guid queueItemId)
        {
            try
            {
                var queueItem = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.Id == queueItemId && q.IsActive);

                if (queueItem == null)
                {
                    return false;
                }

                queueItem.Status = QueueItemStatus.Skipped;
                queueItem.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("跳过号码: 患者 {PatientName}, 队列号 {QueueNumber}", 
                    queueItem.PatientName, queueItem.QueueNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "跳过号码失败: QueueItemId={QueueItemId}", queueItemId);
                throw;
            }
        }

        /// <summary>
        /// 取消排队
        /// </summary>
        public async Task<bool> CancelQueueAsync(Guid queueItemId)
        {
            try
            {
                var queueItem = await _context.Set<QueueItemModel>()
                    .FirstOrDefaultAsync(q => q.Id == queueItemId && q.IsActive);

                if (queueItem == null)
                {
                    return false;
                }

                queueItem.Status = QueueItemStatus.Cancelled;
                queueItem.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("取消排队: 患者 {PatientName}, 队列号 {QueueNumber}", 
                    queueItem.PatientName, queueItem.QueueNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消排队失败: QueueItemId={QueueItemId}", queueItemId);
                throw;
            }
        }

        /// <summary>
        /// 获取患者当前排队状态
        /// </summary>
        public async Task<List<QueueItemModel>> GetPatientCurrentQueuesAsync(Guid patientId)
        {
            try
            {
                return await _context.Set<QueueItemModel>()
                    .Where(q => q.PatientId == patientId && q.IsActive &&
                               (q.Status == QueueItemStatus.Waiting || 
                                q.Status == QueueItemStatus.Called ||
                                q.Status == QueueItemStatus.InService))
                    .OrderBy(q => q.CreateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者当前排队状态失败: PatientId={PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        public async Task<QueueStatistics> GetQueueStatisticsAsync(QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var query = _context.Set<QueueItemModel>()
                    .Where(q => q.QueueType == queueType && q.IsActive);

                if (servicePointId.HasValue)
                {
                    query = query.Where(q => q.ServicePointId == servicePointId.Value);
                }

                var todayQuery = query.Where(q => q.CreateTime >= today && q.CreateTime < tomorrow);

                var waitingCount = await query.CountAsync(q => q.Status == QueueItemStatus.Waiting);
                var inServiceCount = await query.CountAsync(q => q.Status == QueueItemStatus.InService);
                var completedTodayCount = await todayQuery.CountAsync(q => q.Status == QueueItemStatus.Completed);

                // 计算平均等待时间和服务时间
                var completedToday = await todayQuery
                    .Where(q => q.Status == QueueItemStatus.Completed && 
                               q.CallTime.HasValue && q.StartServiceTime.HasValue && q.CompleteServiceTime.HasValue)
                    .ToListAsync();

                var avgWaitTime = 0.0;
                var avgServiceTime = 0.0;
                var maxWaitTime = 0;

                if (completedToday.Any())
                {
                    var waitTimes = completedToday
                        .Select(q => (q.CallTime!.Value - q.QueueTime).TotalMinutes)
                        .ToList();

                    var serviceTimes = completedToday
                        .Select(q => (q.CompleteServiceTime!.Value - q.StartServiceTime!.Value).TotalMinutes)
                        .ToList();

                    avgWaitTime = waitTimes.Average();
                    avgServiceTime = serviceTimes.Average();
                    maxWaitTime = (int)waitTimes.Max();
                }

                return new QueueStatistics
                {
                    WaitingCount = waitingCount,
                    InServiceCount = inServiceCount,
                    CompletedTodayCount = completedTodayCount,
                    AverageWaitTime = avgWaitTime,
                    AverageServiceTime = avgServiceTime,
                    MaxWaitTime = maxWaitTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取队列统计信息失败: QueueType={QueueType}", queueType);
                throw;
            }
        }

        /// <summary>
        /// 自动分配队列号
        /// </summary>
        public async Task<int> GetNextQueueNumberAsync(QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var query = _context.Set<QueueItemModel>()
                    .Where(q => q.QueueType == queueType && 
                               q.CreateTime >= today && 
                               q.CreateTime < tomorrow &&
                               q.IsActive);

                if (servicePointId.HasValue)
                {
                    query = query.Where(q => q.ServicePointId == servicePointId.Value);
                }

                var maxNumber = await query
                    .Select(q => q.QueueNumber)
                    .DefaultIfEmpty(0)
                    .MaxAsync();

                return maxNumber + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取下一个队列号失败: QueueType={QueueType}", queueType);
                throw;
            }
        }

        /// <summary>
        /// 估算等待时间
        /// </summary>
        public async Task<int> EstimateWaitTimeAsync(QueueType queueType, Guid? servicePointId = null)
        {
            try
            {
                var statistics = await GetQueueStatisticsAsync(queueType, servicePointId);
                
                // 简单估算：等待人数 * 平均服务时间
                var avgServiceTime = statistics.AverageServiceTime > 0 ? statistics.AverageServiceTime : 5; // 默认5分钟
                var estimatedTime = (int)(statistics.WaitingCount * avgServiceTime);

                return estimatedTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "估算等待时间失败: QueueType={QueueType}", queueType);
                return 0;
            }
        }

        /// <summary>
        /// 清理过期队列项
        /// </summary>
        public async Task<int> CleanExpiredQueueItemsAsync()
        {
            try
            {
                var yesterday = DateTime.Today.AddDays(-1);
                
                var expiredItems = await _context.Set<QueueItemModel>()
                    .Where(q => q.CreateTime < yesterday && 
                               q.IsActive &&
                               (q.Status == QueueItemStatus.Waiting || 
                                q.Status == QueueItemStatus.Called ||
                                q.Status == QueueItemStatus.Skipped))
                    .ToListAsync();

                foreach (var item in expiredItems)
                {
                    item.Status = QueueItemStatus.Cancelled;
                    item.Remark = $"{item.Remark}\n系统自动取消（过期）";
                    item.UpdateTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("清理过期队列项 {Count} 个", expiredItems.Count);
                return expiredItems.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期队列项失败");
                throw;
            }
        }

        #region Private Methods

        private async Task<string?> GetServicePointNameAsync(QueueType queueType, Guid? servicePointId)
        {
            if (!servicePointId.HasValue)
                return null;

            try
            {
                return queueType switch
                {
                    QueueType.Consultation => (await _context.Doctors.FindAsync(servicePointId.Value))?.Name,
                    QueueType.Pharmacy => $"药房-{servicePointId.Value}", // PharmacyModel没有Name属性
                    QueueType.TreatmentRoom => (await _context.TreatmentRooms.FindAsync(servicePointId.Value))?.Name,
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private int GetPriorityFromRegistration(LYBT.Models.Registration.RegistrationModel? registration)
        {
            if (registration == null)
                return 0;

            // 根据挂号类型设置优先级
            return registration.RegistrationType switch
            {
                RegistrationType.Emergency => 2,
                RegistrationType.Expert => 1,
                _ => 0
            };
        }

        private async Task<QueueItemModel?> GetNextWaitingItemAsync(QueueType queueType, Guid? servicePointId)
        {
            var query = _context.Set<QueueItemModel>()
                .Where(q => q.QueueType == queueType && 
                           q.Status == QueueItemStatus.Waiting && 
                           q.IsActive);

            if (servicePointId.HasValue)
            {
                query = query.Where(q => q.ServicePointId == servicePointId.Value);
            }

            return await query
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.QueueTime)
                .FirstOrDefaultAsync();
        }

        private async Task AutoEnqueueNextStageAsync(QueueItemModel completedItem)
        {
            try
            {
                // 根据完成的服务类型，自动加入下一个队列
                var medicalCase = await _context.MedicalCases
                    .Include(m => m.TreatmentPlan)
                    .FirstOrDefaultAsync(m => m.Id == completedItem.MedicalCaseId);

                if (medicalCase == null)
                    return;

                switch (completedItem.QueueType)
                {
                    case QueueType.Consultation:
                        // 看诊完成，如果有治疗方案，进入缴费队列
                        if (medicalCase.TreatmentPlanId.HasValue)
                        {
                            await AddToQueueAsync(medicalCase.Id, QueueType.Payment);
                        }
                        break;

                    case QueueType.Payment:
                        // 缴费完成，检查是否需要药房或理疗
                        if (medicalCase.TreatmentPlan?.Prescription != null)
                        {
                            await AddToQueueAsync(medicalCase.Id, QueueType.Pharmacy);
                        }
                        if (medicalCase.TreatmentPlan?.PhysiotherapyItems?.Any() == true)
                        {
                            await AddToQueueAsync(medicalCase.Id, QueueType.TreatmentRoom);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "自动加入下一队列失败: QueueItemId={QueueItemId}", completedItem.Id);
            }
        }

        #endregion
    }
}