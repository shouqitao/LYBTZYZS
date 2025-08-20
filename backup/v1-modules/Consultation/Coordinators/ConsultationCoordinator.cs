using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Coordinators
{
    /// <summary>
    /// 看诊业务协调器 - UltraThink架构的业务协调层
    /// 负责看诊流程的业务逻辑协调，包括中医四诊过程管理
    /// </summary>
    public class ConsultationCoordinator
    {
        #region Fields

        private readonly ILogger<ConsultationCoordinator> _logger;
        private readonly Dictionary<Guid, ConsultationSessionData> _activeSessions;
        private readonly Dictionary<Guid, DateTime> _sessionTimeouts;

        #endregion

        #region Constructor

        public ConsultationCoordinator(ILogger<ConsultationCoordinator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new Dictionary<Guid, ConsultationSessionData>();
            _sessionTimeouts = new Dictionary<Guid, DateTime>();
        }

        #endregion

        #region Events

        /// <summary>看诊会话开始事件</summary>
        public event EventHandler<ConsultationSessionStartedEventArgs>? SessionStarted;

        /// <summary>看诊会话结束事件</summary>
        public event EventHandler<ConsultationSessionEndedEventArgs>? SessionEnded;

        /// <summary>四诊状态变更事件</summary>
        public event EventHandler<DiagnosisStatusChangedEventArgs>? DiagnosisStatusChanged;

        /// <summary>处方创建事件</summary>
        public event EventHandler<PrescriptionCreatedEventArgs>? PrescriptionCreated;

        /// <summary>会话超时事件</summary>
        public event EventHandler<ConsultationSessionTimeoutEventArgs>? SessionTimeout;

        #endregion

        #region Session Management

        /// <summary>
        /// 开始看诊会话
        /// </summary>
        public async Task<ServiceResult<Guid>> StartSessionAsync(Guid patientId, Guid doctorId)
        {
            try
            {
                var sessionId = Guid.NewGuid();
                var sessionData = new ConsultationSessionData
                {
                    SessionId = sessionId,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    StartTime = DateTime.Now,
                    Status = ConsultationStatus.InProgress,
                    CurrentStage = DiagnosisStage.Observation, // 中医四诊：望诊开始
                    StageHistory = new List<DiagnosisStageRecord>()
                };

                _activeSessions[sessionId] = sessionData;
                _sessionTimeouts[sessionId] = DateTime.Now.AddHours(2); // 2小时会话超时

                _logger.LogInformation("看诊会话开始: SessionId={SessionId}, PatientId={PatientId}, DoctorId={DoctorId}", 
                    sessionId, patientId, doctorId);

                SessionStarted?.Invoke(this, new ConsultationSessionStartedEventArgs
                {
                    SessionId = sessionId,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    StartTime = sessionData.StartTime
                });

                return ServiceResult<Guid>.Success(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊会话失败: PatientId={PatientId}, DoctorId={DoctorId}", patientId, doctorId);
                return ServiceResult<Guid>.Failure($"开始看诊会话失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 结束看诊会话
        /// </summary>
        public async Task<ServiceResult<bool>> EndSessionAsync(Guid sessionId, string summary)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var sessionData))
                {
                    return ServiceResult<bool>.Failure("找不到指定的看诊会话");
                }

                sessionData.EndTime = DateTime.Now;
                sessionData.Status = ConsultationStatus.Completed;
                sessionData.Summary = summary;
                sessionData.Duration = sessionData.EndTime.Value - sessionData.StartTime;

                _logger.LogInformation("看诊会话结束: SessionId={SessionId}, Duration={Duration}", 
                    sessionId, sessionData.Duration);

                SessionEnded?.Invoke(this, new ConsultationSessionEndedEventArgs
                {
                    SessionId = sessionId,
                    PatientId = sessionData.PatientId,
                    DoctorId = sessionData.DoctorId,
                    EndTime = sessionData.EndTime.Value,
                    Duration = sessionData.Duration,
                    Summary = summary
                });

                // 清理会话数据
                _activeSessions.Remove(sessionId);
                _sessionTimeouts.Remove(sessionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结束看诊会话失败: SessionId={SessionId}", sessionId);
                return ServiceResult<bool>.Failure($"结束看诊会话失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取活跃会话
        /// </summary>
        public ServiceResult<ConsultationSessionData?> GetActiveSession(Guid sessionId)
        {
            try
            {
                var session = _activeSessions.GetValueOrDefault(sessionId);
                return ServiceResult<ConsultationSessionData?>.Success(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃会话失败: SessionId={SessionId}", sessionId);
                return ServiceResult<ConsultationSessionData?>.Failure($"获取活跃会话失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        public ServiceResult<List<ConsultationSessionData>> GetAllActiveSessions()
        {
            try
            {
                var sessions = new List<ConsultationSessionData>(_activeSessions.Values);
                return ServiceResult<List<ConsultationSessionData>>.Success(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有活跃会话失败");
                return ServiceResult<List<ConsultationSessionData>>.Failure($"获取所有活跃会话失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Diagnosis Stage Management (中医四诊管理)

        /// <summary>
        /// 切换诊断阶段
        /// </summary>
        public async Task<ServiceResult<bool>> AdvanceStageAsync(Guid sessionId, DiagnosisStage nextStage, string notes = "")
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var sessionData))
                {
                    return ServiceResult<bool>.Failure("找不到指定的看诊会话");
                }

                var previousStage = sessionData.CurrentStage;
                var stageRecord = new DiagnosisStageRecord
                {
                    Stage = previousStage,
                    StartTime = sessionData.StageHistory.LastOrDefault()?.StartTime ?? sessionData.StartTime,
                    EndTime = DateTime.Now,
                    Notes = notes,
                    CompletedBy = sessionData.DoctorId
                };

                sessionData.StageHistory.Add(stageRecord);
                sessionData.CurrentStage = nextStage;

                _logger.LogInformation("诊断阶段切换: SessionId={SessionId}, From={Previous}, To={Next}", 
                    sessionId, previousStage, nextStage);

                DiagnosisStatusChanged?.Invoke(this, new DiagnosisStatusChangedEventArgs
                {
                    SessionId = sessionId,
                    PreviousStage = previousStage,
                    CurrentStage = nextStage,
                    Notes = notes,
                    Timestamp = DateTime.Now
                });

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换诊断阶段失败: SessionId={SessionId}", sessionId);
                return ServiceResult<bool>.Failure($"切换诊断阶段失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取诊断进度
        /// </summary>
        public ServiceResult<DiagnosisProgress> GetDiagnosisProgress(Guid sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var sessionData))
                {
                    return ServiceResult<DiagnosisProgress>.Failure("找不到指定的看诊会话");
                }

                var progress = new DiagnosisProgress
                {
                    SessionId = sessionId,
                    CurrentStage = sessionData.CurrentStage,
                    CompletedStages = sessionData.StageHistory,
                    OverallProgress = CalculateProgress(sessionData.CurrentStage),
                    EstimatedTimeRemaining = EstimateRemainingTime(sessionData)
                };

                return ServiceResult<DiagnosisProgress>.Success(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊断进度失败: SessionId={SessionId}", sessionId);
                return ServiceResult<DiagnosisProgress>.Failure($"获取诊断进度失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Prescription Management

        /// <summary>
        /// 创建处方
        /// </summary>
        public async Task<ServiceResult<Guid>> CreatePrescriptionAsync(Guid sessionId, PrescriptionData prescriptionData)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var sessionData))
                {
                    return ServiceResult<Guid>.Failure("找不到指定的看诊会话");
                }

                var prescriptionId = Guid.NewGuid();
                prescriptionData.PrescriptionId = prescriptionId;
                prescriptionData.SessionId = sessionId;
                prescriptionData.PatientId = sessionData.PatientId;
                prescriptionData.DoctorId = sessionData.DoctorId;
                prescriptionData.CreateTime = DateTime.Now;

                sessionData.Prescriptions.Add(prescriptionData);

                _logger.LogInformation("处方创建: SessionId={SessionId}, PrescriptionId={PrescriptionId}", 
                    sessionId, prescriptionId);

                PrescriptionCreated?.Invoke(this, new PrescriptionCreatedEventArgs
                {
                    SessionId = sessionId,
                    PrescriptionId = prescriptionId,
                    PatientId = sessionData.PatientId,
                    DoctorId = sessionData.DoctorId,
                    CreateTime = prescriptionData.CreateTime
                });

                return ServiceResult<Guid>.Success(prescriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败: SessionId={SessionId}", sessionId);
                return ServiceResult<Guid>.Failure($"创建处方失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Session Monitoring

        /// <summary>
        /// 检查会话超时
        /// </summary>
        public async Task<ServiceResult<List<Guid>>> CheckSessionTimeoutsAsync()
        {
            try
            {
                var timedOutSessions = new List<Guid>();
                var now = DateTime.Now;

                foreach (var kvp in _sessionTimeouts.ToList())
                {
                    if (now > kvp.Value)
                    {
                        var sessionId = kvp.Key;
                        timedOutSessions.Add(sessionId);

                        _logger.LogWarning("看诊会话超时: SessionId={SessionId}", sessionId);

                        SessionTimeout?.Invoke(this, new ConsultationSessionTimeoutEventArgs
                        {
                            SessionId = sessionId,
                            TimeoutTime = now,
                            SessionData = _activeSessions.GetValueOrDefault(sessionId)
                        });

                        // 自动结束超时会话
                        await EndSessionAsync(sessionId, "会话超时自动结束");
                    }
                }

                return ServiceResult<List<Guid>>.Success(timedOutSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查会话超时失败");
                return ServiceResult<List<Guid>>.Failure($"检查会话超时失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 延长会话时间
        /// </summary>
        public ServiceResult<bool> ExtendSession(Guid sessionId, TimeSpan extension)
        {
            try
            {
                if (!_sessionTimeouts.ContainsKey(sessionId))
                {
                    return ServiceResult<bool>.Failure("找不到指定的会话");
                }

                _sessionTimeouts[sessionId] = _sessionTimeouts[sessionId].Add(extension);

                _logger.LogInformation("会话时间延长: SessionId={SessionId}, Extension={Extension}", 
                    sessionId, extension);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "延长会话时间失败: SessionId={SessionId}", sessionId);
                return ServiceResult<bool>.Failure($"延长会话时间失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private double CalculateProgress(DiagnosisStage currentStage)
        {
            return currentStage switch
            {
                DiagnosisStage.Observation => 0.25,  // 望诊 25%
                DiagnosisStage.Listening => 0.50,    // 闻诊 50%
                DiagnosisStage.Inquiry => 0.75,      // 问诊 75%
                DiagnosisStage.Palpation => 0.90,    // 切诊 90%
                DiagnosisStage.Diagnosis => 1.00,    // 诊断完成 100%
                _ => 0.0
            };
        }

        private TimeSpan EstimateRemainingTime(ConsultationSessionData sessionData)
        {
            var averageStageTime = TimeSpan.FromMinutes(15); // 每个阶段平均15分钟
            var remainingStages = GetRemainingStagesCount(sessionData.CurrentStage);
            return TimeSpan.FromMinutes(remainingStages * averageStageTime.TotalMinutes);
        }

        private int GetRemainingStagesCount(DiagnosisStage currentStage)
        {
            return currentStage switch
            {
                DiagnosisStage.Observation => 4, // 还需要闻问切诊
                DiagnosisStage.Listening => 3,   // 还需要问切诊
                DiagnosisStage.Inquiry => 2,     // 还需要切诊
                DiagnosisStage.Palpation => 1,   // 还需要诊断
                DiagnosisStage.Diagnosis => 0,   // 完成
                _ => 5
            };
        }

        #endregion

        #region IDataCoordinator Implementation

        public Task<ServiceResult<bool>> ValidateAsync(object data)
        {
            // 看诊数据验证逻辑
            return Task.FromResult(ServiceResult<bool>.Success(true));
        }

        public Task<ServiceResult<bool>> CacheAsync(string key, object data, TimeSpan? expiry = null)
        {
            // 看诊数据缓存逻辑
            return Task.FromResult(ServiceResult<bool>.Success(true));
        }

        public Task<ServiceResult<T?>> GetCachedAsync<T>(string key)
        {
            // 获取缓存的看诊数据
            return Task.FromResult(ServiceResult<T?>.Success(default(T)));
        }

        public Task<ServiceResult<bool>> InvalidateCacheAsync(string pattern)
        {
            // 清理看诊相关缓存
            return Task.FromResult(ServiceResult<bool>.Success(true));
        }

        #endregion
    }

    #region Event Args and Data Classes

    /// <summary>看诊会话数据</summary>
    public class ConsultationSessionData
    {
        public Guid SessionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public ConsultationStatus Status { get; set; }
        public DiagnosisStage CurrentStage { get; set; }
        public List<DiagnosisStageRecord> StageHistory { get; set; } = new();
        public List<PrescriptionData> Prescriptions { get; set; } = new();
        public string? Summary { get; set; }
        public Dictionary<string, object> SessionData { get; set; } = new();
    }

    /// <summary>诊断阶段记录</summary>
    public class DiagnosisStageRecord
    {
        public DiagnosisStage Stage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Notes { get; set; } = string.Empty;
        public Guid CompletedBy { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>诊断进度</summary>
    public class DiagnosisProgress
    {
        public Guid SessionId { get; set; }
        public DiagnosisStage CurrentStage { get; set; }
        public List<DiagnosisStageRecord> CompletedStages { get; set; } = new();
        public double OverallProgress { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    /// <summary>处方数据</summary>
    public class PrescriptionData
    {
        public Guid PrescriptionId { get; set; }
        public Guid SessionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreateTime { get; set; }
        public List<HerbPrescriptionItem> Herbs { get; set; } = new();
        public string Instructions { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>药材处方项</summary>
    public class HerbPrescriptionItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }

    /// <summary>诊断状态枚举</summary>
    public enum ConsultationStatus
    {
        Scheduled,    // 已预约
        InProgress,   // 进行中
        Completed,    // 已完成
        Cancelled,    // 已取消
        Timeout       // 超时
    }

    /// <summary>中医四诊阶段</summary>
    public enum DiagnosisStage
    {
        Observation,  // 望诊
        Listening,    // 闻诊
        Inquiry,      // 问诊
        Palpation,    // 切诊
        Diagnosis     // 诊断
    }

    // Event Args Classes
    public class ConsultationSessionStartedEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class ConsultationSessionEndedEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public class DiagnosisStatusChangedEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public DiagnosisStage PreviousStage { get; set; }
        public DiagnosisStage CurrentStage { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class PrescriptionCreatedEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class ConsultationSessionTimeoutEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public DateTime TimeoutTime { get; set; }
        public ConsultationSessionData? SessionData { get; set; }
    }

    #endregion
}