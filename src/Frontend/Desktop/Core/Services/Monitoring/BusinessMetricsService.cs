using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Monitoring
{
    /// <summary>
    /// 业务指标服务 - UltraThink Stage 5.3.1 业务监控组件
    /// 
    /// 核心功能：
    /// 1. 业务操作追踪
    /// 2. 关键指标统计
    /// 3. 业务流程监控
    /// 4. 业务洞察生成
    /// 5. 中医诊所特定指标
    /// </summary>
    public interface IBusinessMetricsService
    {
        /// <summary>
        /// 记录患者操作
        /// </summary>
        void RecordPatientOperation(PatientOperationType operationType, Guid patientId, Dictionary<string, object>? additionalData = null);

        /// <summary>
        /// 记录诊疗操作
        /// </summary>
        void RecordConsultationOperation(ConsultationOperationType operationType, Guid consultationId, TimeSpan? duration = null);

        /// <summary>
        /// 记录处方操作
        /// </summary>
        void RecordPrescriptionOperation(PrescriptionOperationType operationType, Guid prescriptionId, decimal? amount = null);

        /// <summary>
        /// 记录药材使用
        /// </summary>
        void RecordHerbUsage(string herbName, double quantity, string unit, decimal price);

        /// <summary>
        /// 记录业务流程
        /// </summary>
        IBusinessFlowTracker StartBusinessFlow(string flowName);

        /// <summary>
        /// 获取业务指标
        /// </summary>
        BusinessMetricsSummary GetMetricsSummary(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 获取业务洞察
        /// </summary>
        List<BusinessInsight> GetBusinessInsights();

        /// <summary>
        /// 获取热门药材
        /// </summary>
        List<HerbUsageRanking> GetTopHerbs(int count = 10, TimeSpan? period = null);

        /// <summary>
        /// 获取医生工作量
        /// </summary>
        DoctorWorkloadReport GetDoctorWorkload(string doctorId, DateTime? date = null);

        /// <summary>
        /// 获取营收统计
        /// </summary>
        RevenueStatistics GetRevenueStatistics(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// 业务指标服务实现
    /// </summary>
    public class BusinessMetricsService : IBusinessMetricsService, IDisposable
    {
        #region 私有字段

        private readonly ILogger<BusinessMetricsService> _logger;
        private readonly IStructuredLoggingService _structuredLogger;
        
        // 业务数据存储
        private readonly ConcurrentQueue<BusinessEvent> _businessEvents = new();
        private readonly ConcurrentDictionary<string, PatientMetrics> _patientMetrics = new();
        private readonly ConcurrentDictionary<string, ConsultationMetrics> _consultationMetrics = new();
        private readonly ConcurrentDictionary<string, PrescriptionMetrics> _prescriptionMetrics = new();
        private readonly ConcurrentDictionary<string, HerbUsageMetrics> _herbUsageMetrics = new();
        private readonly ConcurrentDictionary<string, DoctorMetrics> _doctorMetrics = new();
        private readonly ConcurrentQueue<BusinessFlow> _completedFlows = new();
        
        // 统计计数器
        private long _totalPatientOperations = 0;
        private long _totalConsultations = 0;
        private long _totalPrescriptions = 0;
        private decimal _totalRevenue = 0;
        
        // 后台任务
        private readonly Timer _insightGenerationTimer;
        private readonly Timer _cleanupTimer;
        
        // 配置
        private readonly BusinessMetricsConfig _config;

        #endregion

        #region 构造函数

        public BusinessMetricsService(
            ILogger<BusinessMetricsService> logger,
            IStructuredLoggingService structuredLogger,
            BusinessMetricsConfig? config = null)
        {
            _logger = logger;
            _structuredLogger = structuredLogger;
            _config = config ?? BusinessMetricsConfig.Default();
            
            // 启动洞察生成定时器
            _insightGenerationTimer = new Timer(
                GenerateInsights,
                null,
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(10));
            
            // 启动清理定时器
            _cleanupTimer = new Timer(
                CleanupOldData,
                null,
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(1));
            
            _logger.LogInformation("业务指标服务已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 记录患者操作
        /// </summary>
        public void RecordPatientOperation(PatientOperationType operationType, Guid patientId, Dictionary<string, object>? additionalData = null)
        {
            Interlocked.Increment(ref _totalPatientOperations);
            
            var businessEvent = new BusinessEvent
            {
                EventType = "PatientOperation",
                EventSubType = operationType.ToString(),
                EntityId = patientId,
                Timestamp = DateTime.Now,
                AdditionalData = additionalData
            };
            
            _businessEvents.Enqueue(businessEvent);
            
            // 更新患者指标
            var dateKey = DateTime.Today.ToString("yyyy-MM-dd");
            _patientMetrics.AddOrUpdate(dateKey,
                new PatientMetrics
                {
                    Date = DateTime.Today,
                    NewPatients = operationType == PatientOperationType.Register ? 1 : 0,
                    ReturningPatients = operationType == PatientOperationType.Return ? 1 : 0,
                    TotalVisits = 1
                },
                (key, existing) =>
                {
                    if (operationType == PatientOperationType.Register) existing.NewPatients++;
                    if (operationType == PatientOperationType.Return) existing.ReturningPatients++;
                    existing.TotalVisits++;
                    return existing;
                });
            
            _structuredLogger.LogBusinessOperation(
                $"患者{operationType}",
                new { PatientId = patientId, AdditionalData = additionalData });
            
            // 限制队列大小
            while (_businessEvents.Count > _config.MaxEventsInMemory)
            {
                _businessEvents.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 记录诊疗操作
        /// </summary>
        public void RecordConsultationOperation(ConsultationOperationType operationType, Guid consultationId, TimeSpan? duration = null)
        {
            Interlocked.Increment(ref _totalConsultations);
            
            var businessEvent = new BusinessEvent
            {
                EventType = "ConsultationOperation",
                EventSubType = operationType.ToString(),
                EntityId = consultationId,
                Timestamp = DateTime.Now,
                Duration = duration
            };
            
            _businessEvents.Enqueue(businessEvent);
            
            // 更新诊疗指标
            var dateKey = DateTime.Today.ToString("yyyy-MM-dd");
            _consultationMetrics.AddOrUpdate(dateKey,
                new ConsultationMetrics
                {
                    Date = DateTime.Today,
                    TotalConsultations = 1,
                    CompletedConsultations = operationType == ConsultationOperationType.Complete ? 1 : 0,
                    TotalDuration = duration ?? TimeSpan.Zero
                },
                (key, existing) =>
                {
                    existing.TotalConsultations++;
                    if (operationType == ConsultationOperationType.Complete) existing.CompletedConsultations++;
                    if (duration.HasValue) existing.TotalDuration += duration.Value;
                    return existing;
                });
            
            _structuredLogger.LogBusinessOperation(
                $"诊疗{operationType}",
                new { ConsultationId = consultationId, Duration = duration });
        }

        /// <summary>
        /// 记录处方操作
        /// </summary>
        public void RecordPrescriptionOperation(PrescriptionOperationType operationType, Guid prescriptionId, decimal? amount = null)
        {
            Interlocked.Increment(ref _totalPrescriptions);
            
            if (amount.HasValue && operationType == PrescriptionOperationType.Paid)
            {
                Interlocked.Add(ref _totalRevenue, (long)(amount.Value * 100)); // 以分为单位存储
            }
            
            var businessEvent = new BusinessEvent
            {
                EventType = "PrescriptionOperation",
                EventSubType = operationType.ToString(),
                EntityId = prescriptionId,
                Timestamp = DateTime.Now,
                Amount = amount
            };
            
            _businessEvents.Enqueue(businessEvent);
            
            // 更新处方指标
            var dateKey = DateTime.Today.ToString("yyyy-MM-dd");
            _prescriptionMetrics.AddOrUpdate(dateKey,
                new PrescriptionMetrics
                {
                    Date = DateTime.Today,
                    TotalPrescriptions = 1,
                    PrintedPrescriptions = operationType == PrescriptionOperationType.Print ? 1 : 0,
                    PaidPrescriptions = operationType == PrescriptionOperationType.Paid ? 1 : 0,
                    TotalAmount = amount ?? 0
                },
                (key, existing) =>
                {
                    existing.TotalPrescriptions++;
                    if (operationType == PrescriptionOperationType.Print) existing.PrintedPrescriptions++;
                    if (operationType == PrescriptionOperationType.Paid) existing.PaidPrescriptions++;
                    if (amount.HasValue) existing.TotalAmount += amount.Value;
                    return existing;
                });
            
            _structuredLogger.LogBusinessOperation(
                $"处方{operationType}",
                new { PrescriptionId = prescriptionId, Amount = amount });
        }

        /// <summary>
        /// 记录药材使用
        /// </summary>
        public void RecordHerbUsage(string herbName, double quantity, string unit, decimal price)
        {
            _herbUsageMetrics.AddOrUpdate(herbName,
                new HerbUsageMetrics
                {
                    HerbName = herbName,
                    TotalQuantity = quantity,
                    Unit = unit,
                    TotalValue = price * (decimal)quantity,
                    UsageCount = 1,
                    LastUsedTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.TotalQuantity += quantity;
                    existing.TotalValue += price * (decimal)quantity;
                    existing.UsageCount++;
                    existing.LastUsedTime = DateTime.Now;
                    return existing;
                });
            
            _structuredLogger.LogBusinessOperation(
                "药材使用",
                new { HerbName = herbName, Quantity = quantity, Unit = unit, Price = price });
        }

        /// <summary>
        /// 记录业务流程
        /// </summary>
        public IBusinessFlowTracker StartBusinessFlow(string flowName)
        {
            return new BusinessFlowTracker(flowName, this);
        }

        /// <summary>
        /// 获取业务指标摘要
        /// </summary>
        public BusinessMetricsSummary GetMetricsSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;
            
            var patientMetrics = _patientMetrics.Values
                .Where(m => m.Date >= start && m.Date <= end)
                .ToList();
            
            var consultationMetrics = _consultationMetrics.Values
                .Where(m => m.Date >= start && m.Date <= end)
                .ToList();
            
            var prescriptionMetrics = _prescriptionMetrics.Values
                .Where(m => m.Date >= start && m.Date <= end)
                .ToList();
            
            return new BusinessMetricsSummary
            {
                StartDate = start,
                EndDate = end,
                TotalPatients = patientMetrics.Sum(m => m.NewPatients),
                TotalVisits = patientMetrics.Sum(m => m.TotalVisits),
                TotalConsultations = consultationMetrics.Sum(m => m.TotalConsultations),
                CompletedConsultations = consultationMetrics.Sum(m => m.CompletedConsultations),
                AverageConsultationDuration = consultationMetrics.Any() && consultationMetrics.Sum(m => m.TotalConsultations) > 0
                    ? TimeSpan.FromMilliseconds(consultationMetrics.Sum(m => m.TotalDuration.TotalMilliseconds) / consultationMetrics.Sum(m => m.TotalConsultations))
                    : TimeSpan.Zero,
                TotalPrescriptions = prescriptionMetrics.Sum(m => m.TotalPrescriptions),
                TotalRevenue = prescriptionMetrics.Sum(m => m.TotalAmount),
                AveragePrescriptionValue = prescriptionMetrics.Sum(m => m.TotalPrescriptions) > 0
                    ? prescriptionMetrics.Sum(m => m.TotalAmount) / prescriptionMetrics.Sum(m => m.TotalPrescriptions)
                    : 0,
                TopHerbs = GetTopHerbs(5, end - start),
                DailyTrends = GenerateDailyTrends(patientMetrics, consultationMetrics, prescriptionMetrics)
            };
        }

        /// <summary>
        /// 获取业务洞察
        /// </summary>
        public List<BusinessInsight> GetBusinessInsights()
        {
            var insights = new List<BusinessInsight>();
            
            // 分析患者趋势
            var recentPatientMetrics = _patientMetrics.Values
                .Where(m => m.Date >= DateTime.Today.AddDays(-7))
                .ToList();
            
            if (recentPatientMetrics.Any())
            {
                var avgNewPatients = recentPatientMetrics.Average(m => m.NewPatients);
                var todayNewPatients = recentPatientMetrics.FirstOrDefault(m => m.Date == DateTime.Today)?.NewPatients ?? 0;
                
                if (todayNewPatients > avgNewPatients * 1.5)
                {
                    insights.Add(new BusinessInsight
                    {
                        Type = InsightType.Trend,
                        Category = "患者",
                        Title = "新患者增长",
                        Description = $"今日新患者数量({todayNewPatients})显著高于7日平均值({avgNewPatients:F0})",
                        Impact = InsightImpact.Positive,
                        GeneratedAt = DateTime.Now
                    });
                }
            }
            
            // 分析处方价值
            var recentPrescriptions = _prescriptionMetrics.Values
                .Where(m => m.Date >= DateTime.Today.AddDays(-30))
                .ToList();
            
            if (recentPrescriptions.Any() && recentPrescriptions.Sum(m => m.TotalPrescriptions) > 0)
            {
                var avgValue = recentPrescriptions.Sum(m => m.TotalAmount) / recentPrescriptions.Sum(m => m.TotalPrescriptions);
                
                if (avgValue < 100)
                {
                    insights.Add(new BusinessInsight
                    {
                        Type = InsightType.Warning,
                        Category = "处方",
                        Title = "处方均价偏低",
                        Description = $"近30天处方均价({avgValue:C})低于预期，建议关注处方质量",
                        Impact = InsightImpact.Negative,
                        Recommendation = "考虑推广高价值的经典验方",
                        GeneratedAt = DateTime.Now
                    });
                }
            }
            
            // 分析热门药材
            var topHerbs = GetTopHerbs(3, TimeSpan.FromDays(7));
            if (topHerbs.Any())
            {
                insights.Add(new BusinessInsight
                {
                    Type = InsightType.Information,
                    Category = "药材",
                    Title = "热门药材",
                    Description = $"本周最常用药材：{string.Join("、", topHerbs.Select(h => h.HerbName))}",
                    Impact = InsightImpact.Neutral,
                    Recommendation = "确保热门药材库存充足",
                    GeneratedAt = DateTime.Now
                });
            }
            
            // 分析工作效率
            var recentConsultations = _consultationMetrics.Values
                .Where(m => m.Date >= DateTime.Today.AddDays(-7))
                .ToList();
            
            if (recentConsultations.Any() && recentConsultations.Sum(m => m.CompletedConsultations) > 0)
            {
                var avgDuration = TimeSpan.FromMilliseconds(
                    recentConsultations.Sum(m => m.TotalDuration.TotalMilliseconds) / 
                    recentConsultations.Sum(m => m.CompletedConsultations));
                
                if (avgDuration > TimeSpan.FromMinutes(30))
                {
                    insights.Add(new BusinessInsight
                    {
                        Type = InsightType.Optimization,
                        Category = "效率",
                        Title = "诊疗时间较长",
                        Description = $"平均诊疗时间({avgDuration.TotalMinutes:F0}分钟)超过30分钟",
                        Impact = InsightImpact.Neutral,
                        Recommendation = "考虑优化诊疗流程或增加医生资源",
                        GeneratedAt = DateTime.Now
                    });
                }
            }
            
            return insights.OrderByDescending(i => i.GeneratedAt).ToList();
        }

        /// <summary>
        /// 获取热门药材
        /// </summary>
        public List<HerbUsageRanking> GetTopHerbs(int count = 10, TimeSpan? period = null)
        {
            var cutoffTime = period.HasValue ? DateTime.Now - period.Value : DateTime.MinValue;
            
            return _herbUsageMetrics.Values
                .Where(h => h.LastUsedTime >= cutoffTime)
                .OrderByDescending(h => h.UsageCount)
                .Take(count)
                .Select((h, index) => new HerbUsageRanking
                {
                    Rank = index + 1,
                    HerbName = h.HerbName,
                    UsageCount = h.UsageCount,
                    TotalQuantity = h.TotalQuantity,
                    Unit = h.Unit,
                    TotalValue = h.TotalValue,
                    AveragePrice = h.TotalValue / (decimal)h.TotalQuantity
                })
                .ToList();
        }

        /// <summary>
        /// 获取医生工作量
        /// </summary>
        public DoctorWorkloadReport GetDoctorWorkload(string doctorId, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var dateKey = $"{doctorId}_{targetDate:yyyy-MM-dd}";
            
            if (_doctorMetrics.TryGetValue(dateKey, out var metrics))
            {
                return new DoctorWorkloadReport
                {
                    DoctorId = doctorId,
                    Date = targetDate,
                    TotalPatients = metrics.PatientCount,
                    TotalConsultations = metrics.ConsultationCount,
                    TotalPrescriptions = metrics.PrescriptionCount,
                    TotalWorkingHours = metrics.TotalWorkingTime.TotalHours,
                    AverageConsultationTime = metrics.ConsultationCount > 0
                        ? TimeSpan.FromMilliseconds(metrics.TotalConsultationTime.TotalMilliseconds / metrics.ConsultationCount)
                        : TimeSpan.Zero,
                    Revenue = metrics.TotalRevenue
                };
            }
            
            return new DoctorWorkloadReport
            {
                DoctorId = doctorId,
                Date = targetDate,
                TotalPatients = 0,
                TotalConsultations = 0,
                TotalPrescriptions = 0,
                TotalWorkingHours = 0,
                AverageConsultationTime = TimeSpan.Zero,
                Revenue = 0
            };
        }

        /// <summary>
        /// 获取营收统计
        /// </summary>
        public RevenueStatistics GetRevenueStatistics(DateTime startDate, DateTime endDate)
        {
            var prescriptionRevenue = _prescriptionMetrics.Values
                .Where(m => m.Date >= startDate && m.Date <= endDate)
                .Sum(m => m.TotalAmount);
            
            var dailyRevenue = _prescriptionMetrics.Values
                .Where(m => m.Date >= startDate && m.Date <= endDate)
                .GroupBy(m => m.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Amount = g.Sum(m => m.TotalAmount),
                    PrescriptionCount = g.Sum(m => m.PaidPrescriptions)
                })
                .OrderBy(d => d.Date)
                .ToList();
            
            var topRevenueHerbs = _herbUsageMetrics.Values
                .OrderByDescending(h => h.TotalValue)
                .Take(10)
                .Select(h => new HerbRevenue
                {
                    HerbName = h.HerbName,
                    TotalRevenue = h.TotalValue,
                    UsageCount = h.UsageCount
                })
                .ToList();
            
            return new RevenueStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = prescriptionRevenue,
                AverageDailyRevenue = dailyRevenue.Any() ? dailyRevenue.Average(d => d.Amount) : 0,
                PeakRevenue = dailyRevenue.Any() ? dailyRevenue.Max(d => d.Amount) : 0,
                DailyRevenue = dailyRevenue,
                TopRevenueHerbs = topRevenueHerbs
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成洞察
        /// </summary>
        private void GenerateInsights(object? state)
        {
            try
            {
                var insights = GetBusinessInsights();
                
                foreach (var insight in insights.Where(i => i.Impact != InsightImpact.Neutral))
                {
                    _structuredLogger.LogBusinessOperation(
                        "业务洞察生成",
                        new
                        {
                            Type = insight.Type,
                            Category = insight.Category,
                            Title = insight.Title,
                            Impact = insight.Impact
                        });
                }
                
                _logger.LogInformation("生成了 {Count} 条业务洞察", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成业务洞察时发生错误");
            }
        }

        /// <summary>
        /// 清理旧数据
        /// </summary>
        private void CleanupOldData(object? state)
        {
            try
            {
                var cutoffTime = DateTime.Now - _config.DataRetentionPeriod;
                
                // 清理业务事件
                var eventsToKeep = new List<BusinessEvent>();
                while (_businessEvents.TryDequeue(out var evt))
                {
                    if (evt.Timestamp >= cutoffTime)
                    {
                        eventsToKeep.Add(evt);
                    }
                }
                
                foreach (var evt in eventsToKeep)
                {
                    _businessEvents.Enqueue(evt);
                }
                
                // 清理过期的指标
                var cutoffDate = DateTime.Today.AddDays(-_config.DataRetentionPeriod.TotalDays);
                var cutoffDateKey = cutoffDate.ToString("yyyy-MM-dd");
                
                foreach (var key in _patientMetrics.Keys.Where(k => string.Compare(k, cutoffDateKey) < 0).ToList())
                {
                    _patientMetrics.TryRemove(key, out _);
                }
                
                foreach (var key in _consultationMetrics.Keys.Where(k => string.Compare(k, cutoffDateKey) < 0).ToList())
                {
                    _consultationMetrics.TryRemove(key, out _);
                }
                
                foreach (var key in _prescriptionMetrics.Keys.Where(k => string.Compare(k, cutoffDateKey) < 0).ToList())
                {
                    _prescriptionMetrics.TryRemove(key, out _);
                }
                
                _logger.LogDebug("业务数据清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理业务数据时发生错误");
            }
        }

        /// <summary>
        /// 生成每日趋势
        /// </summary>
        private List<DailyTrend> GenerateDailyTrends(
            List<PatientMetrics> patientMetrics,
            List<ConsultationMetrics> consultationMetrics,
            List<PrescriptionMetrics> prescriptionMetrics)
        {
            var dates = patientMetrics.Select(m => m.Date)
                .Union(consultationMetrics.Select(m => m.Date))
                .Union(prescriptionMetrics.Select(m => m.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            
            return dates.Select(date => new DailyTrend
            {
                Date = date,
                Patients = patientMetrics.FirstOrDefault(m => m.Date == date)?.TotalVisits ?? 0,
                Consultations = consultationMetrics.FirstOrDefault(m => m.Date == date)?.TotalConsultations ?? 0,
                Prescriptions = prescriptionMetrics.FirstOrDefault(m => m.Date == date)?.TotalPrescriptions ?? 0,
                Revenue = prescriptionMetrics.FirstOrDefault(m => m.Date == date)?.TotalAmount ?? 0
            }).ToList();
        }

        /// <summary>
        /// 记录流程完成
        /// </summary>
        internal void RecordFlowCompletion(BusinessFlow flow)
        {
            _completedFlows.Enqueue(flow);
            
            while (_completedFlows.Count > _config.MaxFlowsInMemory)
            {
                _completedFlows.TryDequeue(out _);
            }
            
            _structuredLogger.LogBusinessOperation(
                $"业务流程完成: {flow.FlowName}",
                new
                {
                    Duration = flow.Duration,
                    StepCount = flow.Steps.Count,
                    Success = flow.Success
                });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _insightGenerationTimer?.Dispose();
            _cleanupTimer?.Dispose();
            
            _logger.LogInformation(
                "业务指标服务已释放 - 患者操作: {Patients}, 诊疗: {Consultations}, 处方: {Prescriptions}, 营收: {Revenue:C}",
                _totalPatientOperations, _totalConsultations, _totalPrescriptions, _totalRevenue / 100m);
        }

        #endregion
    }

    #region 数据模型和辅助类

    /// <summary>
    /// 业务流程追踪器
    /// </summary>
    public class BusinessFlowTracker : IBusinessFlowTracker
    {
        private readonly string _flowName;
        private readonly BusinessMetricsService _service;
        private readonly BusinessFlow _flow;
        private readonly DateTime _startTime;

        public BusinessFlowTracker(string flowName, BusinessMetricsService service)
        {
            _flowName = flowName;
            _service = service;
            _startTime = DateTime.Now;
            _flow = new BusinessFlow
            {
                FlowName = flowName,
                StartTime = _startTime,
                Steps = new List<BusinessFlowStep>()
            };
        }

        public void RecordStep(string stepName, bool success = true, Dictionary<string, object>? data = null)
        {
            _flow.Steps.Add(new BusinessFlowStep
            {
                StepName = stepName,
                Timestamp = DateTime.Now,
                Success = success,
                Data = data
            });
        }

        public void Complete(bool success = true)
        {
            _flow.EndTime = DateTime.Now;
            _flow.Success = success;
            _service.RecordFlowCompletion(_flow);
        }

        public void Dispose()
        {
            if (_flow.EndTime == null)
            {
                Complete(false);
            }
        }
    }

    /// <summary>
    /// 业务流程追踪器接口
    /// </summary>
    public interface IBusinessFlowTracker : IDisposable
    {
        void RecordStep(string stepName, bool success = true, Dictionary<string, object>? data = null);
        void Complete(bool success = true);
    }

    /// <summary>
    /// 业务事件
    /// </summary>
    internal class BusinessEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string EventSubType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan? Duration { get; set; }
        public decimal? Amount { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// 业务流程
    /// </summary>
    internal class BusinessFlow
    {
        public string FlowName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<BusinessFlowStep> Steps { get; set; } = new();
        public bool Success { get; set; }
        
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
    }

    /// <summary>
    /// 业务流程步骤
    /// </summary>
    internal class BusinessFlowStep
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// 患者指标
    /// </summary>
    internal class PatientMetrics
    {
        public DateTime Date { get; set; }
        public int NewPatients { get; set; }
        public int ReturningPatients { get; set; }
        public int TotalVisits { get; set; }
    }

    /// <summary>
    /// 诊疗指标
    /// </summary>
    internal class ConsultationMetrics
    {
        public DateTime Date { get; set; }
        public int TotalConsultations { get; set; }
        public int CompletedConsultations { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// 处方指标
    /// </summary>
    internal class PrescriptionMetrics
    {
        public DateTime Date { get; set; }
        public int TotalPrescriptions { get; set; }
        public int PrintedPrescriptions { get; set; }
        public int PaidPrescriptions { get; set; }
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// 药材使用指标
    /// </summary>
    internal class HerbUsageMetrics
    {
        public string HerbName { get; set; } = string.Empty;
        public double TotalQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsedTime { get; set; }
    }

    /// <summary>
    /// 医生指标
    /// </summary>
    internal class DoctorMetrics
    {
        public string DoctorId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PatientCount { get; set; }
        public int ConsultationCount { get; set; }
        public int PrescriptionCount { get; set; }
        public TimeSpan TotalWorkingTime { get; set; }
        public TimeSpan TotalConsultationTime { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// 患者操作类型
    /// </summary>
    public enum PatientOperationType
    {
        Register,    // 注册
        Update,      // 更新
        View,        // 查看
        Search,      // 搜索
        Return,      // 复诊
        Archive      // 归档
    }

    /// <summary>
    /// 诊疗操作类型
    /// </summary>
    public enum ConsultationOperationType
    {
        Create,      // 创建
        Start,       // 开始
        Diagnose,    // 诊断
        Complete,    // 完成
        Cancel,      // 取消
        Update       // 更新
    }

    /// <summary>
    /// 处方操作类型
    /// </summary>
    public enum PrescriptionOperationType
    {
        Create,      // 创建
        Edit,        // 编辑
        Print,       // 打印
        Cancel,      // 取消
        Paid,        // 已付款
        Dispense     // 配药
    }

    /// <summary>
    /// 业务指标摘要
    /// </summary>
    public class BusinessMetricsSummary
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalPatients { get; set; }
        public int TotalVisits { get; set; }
        public int TotalConsultations { get; set; }
        public int CompletedConsultations { get; set; }
        public TimeSpan AverageConsultationDuration { get; set; }
        public int TotalPrescriptions { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrescriptionValue { get; set; }
        public List<HerbUsageRanking> TopHerbs { get; set; } = new();
        public List<DailyTrend> DailyTrends { get; set; } = new();
    }

    /// <summary>
    /// 药材使用排名
    /// </summary>
    public class HerbUsageRanking
    {
        public int Rank { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public double TotalQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    /// <summary>
    /// 每日趋势
    /// </summary>
    public class DailyTrend
    {
        public DateTime Date { get; set; }
        public int Patients { get; set; }
        public int Consultations { get; set; }
        public int Prescriptions { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// 业务洞察
    /// </summary>
    public class BusinessInsight
    {
        public InsightType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InsightImpact Impact { get; set; }
        public string? Recommendation { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 洞察类型
    /// </summary>
    public enum InsightType
    {
        Trend,          // 趋势
        Warning,        // 警告
        Opportunity,    // 机会
        Optimization,   // 优化建议
        Information     // 信息
    }

    /// <summary>
    /// 洞察影响
    /// </summary>
    public enum InsightImpact
    {
        Positive,   // 正面
        Negative,   // 负面
        Neutral     // 中性
    }

    /// <summary>
    /// 医生工作量报告
    /// </summary>
    public class DoctorWorkloadReport
    {
        public string DoctorId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TotalPatients { get; set; }
        public int TotalConsultations { get; set; }
        public int TotalPrescriptions { get; set; }
        public double TotalWorkingHours { get; set; }
        public TimeSpan AverageConsultationTime { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// 营收统计
    /// </summary>
    public class RevenueStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageDailyRevenue { get; set; }
        public decimal PeakRevenue { get; set; }
        public List<DailyRevenue> DailyRevenue { get; set; } = new();
        public List<HerbRevenue> TopRevenueHerbs { get; set; } = new();
    }

    /// <summary>
    /// 每日营收
    /// </summary>
    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int PrescriptionCount { get; set; }
    }

    /// <summary>
    /// 药材营收
    /// </summary>
    public class HerbRevenue
    {
        public string HerbName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// 业务指标配置
    /// </summary>
    public class BusinessMetricsConfig
    {
        public int MaxEventsInMemory { get; set; } = 10000;
        public int MaxFlowsInMemory { get; set; } = 1000;
        public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
        
        public static BusinessMetricsConfig Default() => new();
    }

    #endregion
}