using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Services.Session;

/// <summary>
/// 统一的会话管理服务实现
/// Phase 2重构：合并SessionManager和UserSessionManager，提供完整的会话管理功能
/// 线程安全的实现，支持事件驱动的状态变化通知
/// </summary>
public class UnifiedSessionManager : IUnifiedSessionManager, IDisposable
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UnifiedSessionManager> _logger;
    private readonly object _stateLock = new();
    private readonly ConcurrentDictionary<string, object> _cache = new();
    
    // 用户会话状态
    private UserDto? _currentUser;
    private string? _token;
    private DateTime? _loginTime;
    
    // 患者会话状态
    private PatientDto? _currentPatient;
    
    // 诊疗会话状态
    private ConsultationDto? _activeConsultation;
    private Guid? _currentMedicalCaseId;
    private ConsultationStatus _consultationStatus = ConsultationStatus.NotStarted;
    
    private bool _disposed = false;

    public UnifiedSessionManager(
        IPermissionService permissionService,
        ILogger<UnifiedSessionManager> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("UnifiedSessionManager initialized - Phase 2 架构");
    }

    #region 用户会话管理

    public UserDto? CurrentUser
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUser;
            }
        }
    }

    public bool IsLoggedIn
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUser != null && !string.IsNullOrEmpty(_token);
            }
        }
    }
    public DateTime? LoginTime
    {
        get
        {
            lock (_stateLock)
            {
                return _loginTime;
            }
        }
    }

    public string? Token
    {
        get
        {
            lock (_stateLock)
            {
                return _token;
            }
        }
    }

    public void SetUserSession(UserDto user, string token)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token cannot be null or empty", nameof(token));

        UserDto? previousUser;
        lock (_stateLock)
        {
            previousUser = _currentUser;
            _currentUser = user;
            _token = token;
            _loginTime = DateTime.Now;
            _cache.Clear(); // 清除权限缓存
        }

        _logger.LogInformation("用户会话已设置: {Username} (ID: {UserId})", user.Username, user.Id);

        // 触发事件
        UserSessionChanged?.Invoke(this, new UserSessionChangedEventArgs
        {
            PreviousUser = previousUser,
            CurrentUser = user,
            Reason = SessionChangeReason.Login
        });
    }

    public void ClearUserSession()
    {
        UserDto? previousUser;
        lock (_stateLock)
        {
            previousUser = _currentUser;
            _currentUser = null;
            _token = null;
            _loginTime = null;
            _cache.Clear();
            
            // 同时清除患者和诊疗会话
            _currentPatient = null;
            _activeConsultation = null;
            _currentMedicalCaseId = null;
            _consultationStatus = ConsultationStatus.NotStarted;
        }

        _logger.LogInformation("用户会话已清除: {Username}", previousUser?.Username);

        // 触发事件
        UserSessionChanged?.Invoke(this, new UserSessionChangedEventArgs
        {
            PreviousUser = previousUser,
            CurrentUser = null,
            Reason = SessionChangeReason.Logout
        });
    }

    public string? GetToken()
    {
        lock (_stateLock)
        {
            return _token;
        }
    }

    public void SetToken(string token)
    {
        lock (_stateLock)
        {
            _token = token;
        }
    }

    public void ClearToken()
    {
        lock (_stateLock)
        {
            _token = null;
        }
    }

    public void RefreshUserInfo(UserDto user)
    {
        if (user == null) return;
        
        UserDto? previousUser;
        lock (_stateLock)
        {
            if (_currentUser?.Id == user.Id)
            {
                previousUser = _currentUser;
                _currentUser = user;
                _cache.Clear(); // 清除权限缓存
            }
            else
            {
                return; // 只能刷新当前用户的信息
            }
        }

        _logger.LogInformation("用户信息已刷新: {Username}", user.Username);

        // 触发事件
        UserSessionChanged?.Invoke(this, new UserSessionChangedEventArgs
        {
            PreviousUser = previousUser,
            CurrentUser = user,
            Reason = SessionChangeReason.UserInfoUpdated
        });
    }

    #endregion

    #region 权限管理

    public UserRole? GetUserRole()
    {
        lock (_stateLock)
        {
            return _currentUser?.Role;
        }
    }

    public bool HasRole(UserRole role)
    {
        var userRole = GetUserRole();
        return userRole == role;
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        
        var user = CurrentUser;
        if (user == null) return false;

        // 使用缓存提升性能
        var cacheKey = $"permission:{user.Id}:{permission}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return (bool)cached;
        }

        var result = _permissionService.HasPermission(user, permission);
        _cache.TryAdd(cacheKey, result);
        return result;
    }

    public bool HasManagementAccess()
    {
        var role = GetUserRole();
        return role.HasValue && _permissionService.HasManagementAccess(role.Value);
    }

    public bool HasMedicalAccess()
    {
        var role = GetUserRole();
        return role.HasValue && _permissionService.HasMedicalAccess(role.Value);
    }

    public IEnumerable<string> GetAccessibleModules()
    {
        var role = GetUserRole();
        if (!role.HasValue) return Enumerable.Empty<string>();

        // 使用缓存
        var cacheKey = $"modules:{role.Value}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return (IEnumerable<string>)cached;
        }

        var modules = _permissionService.GetAccessibleModules(role.Value);
        _cache.TryAdd(cacheKey, modules);
        return modules;
    }

    #endregion

    #region 患者会话管理

    public PatientDto? CurrentPatient
    {
        get
        {
            lock (_stateLock)
            {
                return _currentPatient;
            }
        }
        set => SelectPatient(value);
    }

    public void SelectPatient(PatientDto? patient)
    {
        PatientDto? previousPatient;
        lock (_stateLock)
        {
            previousPatient = _currentPatient;
            _currentPatient = patient;
        }

        if (!ReferenceEquals(previousPatient, patient))
        {
            _logger.LogInformation("患者选择变化: {PreviousId} -> {CurrentId}", 
                previousPatient?.Id, patient?.Id);
            
            // 触发事件
            PatientSelectionChanged?.Invoke(this, new PatientSelectionChangedEventArgs
            {
                PreviousPatient = previousPatient,
                CurrentPatient = patient,
                Reason = SelectionChangeReason.UserSelection
            });
        }
    }

    public void ClearPatientSelection()
    {
        SelectPatient(null);
    }

    #endregion

    #region 诊疗会话管理

    public ConsultationDto? ActiveConsultation
    {
        get
        {
            lock (_stateLock)
            {
                return _activeConsultation;
            }
        }
        set
        {
            ConsultationDto? previousConsultation;
            lock (_stateLock)
            {
                previousConsultation = _activeConsultation;
                _activeConsultation = value;
                
                if (value != null)
                {
                    _consultationStatus = ConsultationStatus.InProgress;
                }
                else
                {
                    _consultationStatus = ConsultationStatus.NotStarted;
                }
            }

            if (!ReferenceEquals(previousConsultation, value))
            {
                ConsultationSessionChanged?.Invoke(this, new ConsultationSessionChangedEventArgs
                {
                    PreviousConsultation = previousConsultation,
                    CurrentConsultation = value,
                    PreviousStatus = _consultationStatus,
                    CurrentStatus = _consultationStatus
                });
            }
        }
    }

    public Guid? CurrentMedicalCaseId
    {
        get
        {
            lock (_stateLock)
            {
                return _currentMedicalCaseId;
            }
        }
        set
        {
            lock (_stateLock)
            {
                _currentMedicalCaseId = value;
            }
        }
    }

    public ConsultationStatus ConsultationStatus
    {
        get
        {
            lock (_stateLock)
            {
                return _consultationStatus;
            }
        }
    }

    public bool HasActiveConsultation
    {
        get
        {
            lock (_stateLock)
            {
                return _activeConsultation != null && _consultationStatus == ConsultationStatus.InProgress;
            }
        }
    }

    public void StartConsultation(PatientDto patient, Guid? medicalCaseId = null)
    {
        if (patient == null) throw new ArgumentNullException(nameof(patient));
        if (!IsLoggedIn) throw new InvalidOperationException("用户未登录，无法开始诊疗");

        ConsultationDto? previousConsultation;
        ConsultationStatus previousStatus;
        
        lock (_stateLock)
        {
            previousConsultation = _activeConsultation;
            previousStatus = _consultationStatus;
            
            // 设置患者
            _currentPatient = patient;
            _currentMedicalCaseId = medicalCaseId ?? Guid.NewGuid();
            
            // 创建新的诊疗会话
            _activeConsultation = new ConsultationDto
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                UserId = _currentUser!.Id,
                MedicalCaseId = _currentMedicalCaseId.Value,
                ConsultationStatus = Shared.Models.Enums.ConsultationStatus.InProgress,
                CreateTime = DateTime.Now
            };
            
            _consultationStatus = ConsultationStatus.InProgress;
        }

        _logger.LogInformation("开始诊疗会话: Patient={PatientId}, MedicalCase={MedicalCaseId}", 
            patient.Id, _currentMedicalCaseId);
        
        // 触发事件
        ConsultationSessionChanged?.Invoke(this, new ConsultationSessionChangedEventArgs
        {
            PreviousConsultation = previousConsultation,
            CurrentConsultation = _activeConsultation,
            PreviousStatus = previousStatus,
            CurrentStatus = _consultationStatus
        });
    }

    public void EndConsultation()
    {
        ConsultationDto? previousConsultation;
        ConsultationStatus previousStatus;
        
        lock (_stateLock)
        {
            previousConsultation = _activeConsultation;
            previousStatus = _consultationStatus;
            
            if (_activeConsultation != null)
            {
                _activeConsultation.ConsultationStatus = Shared.Models.Enums.ConsultationStatus.Completed;
            }
            
            _consultationStatus = ConsultationStatus.Completed;
        }

        _logger.LogInformation("结束诊疗会话: {ConsultationId}", previousConsultation?.Id);
        
        // 触发事件
        ConsultationSessionChanged?.Invoke(this, new ConsultationSessionChangedEventArgs
        {
            PreviousConsultation = previousConsultation,
            CurrentConsultation = _activeConsultation,
            PreviousStatus = previousStatus,
            CurrentStatus = _consultationStatus
        });
    }

    public void UpdateConsultationStatus(ConsultationStatus status)
    {
        ConsultationStatus previousStatus;
        lock (_stateLock)
        {
            previousStatus = _consultationStatus;
            _consultationStatus = status;
            
            if (_activeConsultation != null)
            {
                _activeConsultation.ConsultationStatus = MapToSharedStatus(status);
            }
        }

        if (previousStatus != status)
        {
            _logger.LogInformation("诊疗状态变化: {PreviousStatus} -> {CurrentStatus}", 
                previousStatus, status);
            
            ConsultationSessionChanged?.Invoke(this, new ConsultationSessionChangedEventArgs
            {
                PreviousConsultation = _activeConsultation,
                CurrentConsultation = _activeConsultation,
                PreviousStatus = previousStatus,
                CurrentStatus = status
            });
        }
    }

    #endregion

    #region 会话重置和清理

    public void Reset()
    {
        lock (_stateLock)
        {
            _currentUser = null;
            _token = null;
            _loginTime = null;
            _currentPatient = null;
            _activeConsultation = null;
            _currentMedicalCaseId = null;
            _consultationStatus = ConsultationStatus.NotStarted;
            _cache.Clear();
        }

        _logger.LogInformation("会话状态已重置");
    }

    public void ResetAll()
    {
        Reset(); // ResetAll方法直接调用Reset方法
    }

    public SessionState GetSessionState()
    {
        lock (_stateLock)
        {
            return new SessionState
            {
                User = _currentUser,
                Token = _token,
                LoginTime = _loginTime,
                Patient = _currentPatient,
                Consultation = _activeConsultation,
                MedicalCaseId = _currentMedicalCaseId,
                Status = _consultationStatus,
                CapturedAt = DateTime.Now
            };
        }
    }

    public void RestoreSessionState(SessionState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        lock (_stateLock)
        {
            _currentUser = state.User;
            _token = state.Token;
            _loginTime = state.LoginTime;
            _currentPatient = state.Patient;
            _activeConsultation = state.Consultation;
            _currentMedicalCaseId = state.MedicalCaseId;
            _consultationStatus = state.Status;
            _cache.Clear();
        }

        _logger.LogInformation("会话状态已恢复: User={Username}, Patient={PatientId}", 
            state.User?.Username, state.Patient?.Id);
    }

    #endregion

    #region 事件定义

    public event EventHandler<UserSessionChangedEventArgs>? UserSessionChanged;
    public event EventHandler<PatientSelectionChangedEventArgs>? PatientSelectionChanged;
    public event EventHandler<ConsultationSessionChangedEventArgs>? ConsultationSessionChanged;
    public event EventHandler<PermissionChangedEventArgs>? PermissionChanged;
    public event EventHandler<SessionMessageEventArgs>? SessionMessage;

    #endregion

    #region 辅助方法

    private static Shared.Models.Enums.ConsultationStatus MapToSharedStatus(ConsultationStatus status)
    {
        return status switch
        {
            ConsultationStatus.NotStarted => Shared.Models.Enums.ConsultationStatus.Pending,
            ConsultationStatus.InProgress => Shared.Models.Enums.ConsultationStatus.InProgress,
            ConsultationStatus.Paused => Shared.Models.Enums.ConsultationStatus.InProgress,
            ConsultationStatus.Completed => Shared.Models.Enums.ConsultationStatus.Completed,
            ConsultationStatus.Cancelled => Shared.Models.Enums.ConsultationStatus.Cancelled,
            _ => Shared.Models.Enums.ConsultationStatus.Pending
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Clear();
            _disposed = true;
            _logger.LogDebug("UnifiedSessionManager disposed");
        }
    }

    #endregion
}