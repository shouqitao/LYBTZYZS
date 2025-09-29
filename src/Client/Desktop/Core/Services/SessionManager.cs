using System;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Events;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using SessionStatus = LYBT.Desktop.Core.Interfaces.Services.ConsultationStatus;
using StatusShared = LYBT.Shared.Models.Enums.ConsultationStatus;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// Simple in-memory session manager implementing ISessionManager.
    /// Keeps track of current user, patient and consultation state.
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly object _gate = new();

        private PatientDto? _currentPatient;
        private ConsultationDto? _activeConsultation;
        private UserDto? _currentUser;
        private Guid? _currentMedicalCaseId;
        private SessionStatus _consultationStatus = SessionStatus.NotStarted;

        public event EventHandler<PatientChangedEventArgs>? PatientChanged;
        public event EventHandler<ConsultationChangedEventArgs>? ConsultationChanged;
        public event EventHandler<UserChangedEventArgs>? UserChanged;
        public event EventHandler<StatusMessageEventArgs>? StatusMessage;

        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set
            {
                lock (_gate)
                {
                    if (!ReferenceEquals(_currentPatient, value))
                    {
                        var old = _currentPatient;
                        _currentPatient = value;
                        PatientChanged?.Invoke(this, new PatientChangedEventArgs
                        {
                            OldPatient = old,
                            NewPatient = _currentPatient,
                            ChangedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        public ConsultationDto? ActiveConsultation
        {
            get => _activeConsultation;
            set
            {
                lock (_gate)
                {
                    if (!ReferenceEquals(_activeConsultation, value))
                    {
                        var old = _activeConsultation;
                        var oldStatus = _consultationStatus;
                        _activeConsultation = value;
                        _consultationStatus = value == null ? SessionStatus.NotStarted : MapFromShared(value.ConsultationStatus);
                        ConsultationChanged?.Invoke(this, new ConsultationChangedEventArgs
                        {
                            OldConsultation = old,
                            NewConsultation = _activeConsultation,
                            OldStatus = oldStatus,
                            NewStatus = _consultationStatus,
                            ChangedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        public UserDto? CurrentUser
        {
            get => _currentUser;
            set
            {
                lock (_gate)
                {
                    if (!ReferenceEquals(_currentUser, value))
                    {
                        var old = _currentUser;
                        _currentUser = value;
                        UserChanged?.Invoke(this, new UserChangedEventArgs
                        {
                            OldUser = old,
                            NewUser = _currentUser,
                            IsLogin = _currentUser != null,
                            ChangedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        public Guid? CurrentMedicalCaseId
        {
            get => _currentMedicalCaseId;
            set => _currentMedicalCaseId = value;
        }

        public SessionStatus ConsultationStatus
        {
            get => _consultationStatus;
            set
            {
                lock (_gate)
                {
                    if (_consultationStatus != value)
                    {
                        var oldStatus = _consultationStatus;
                        _consultationStatus = value;
                        ConsultationChanged?.Invoke(this, new ConsultationChangedEventArgs
                        {
                            OldConsultation = _activeConsultation,
                            NewConsultation = _activeConsultation,
                            OldStatus = oldStatus,
                            NewStatus = _consultationStatus,
                            ChangedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        public void StartConsultation(PatientDto patient, Guid? medicalCaseId = null)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));

            lock (_gate)
            {
                CurrentPatient = patient;
                _currentMedicalCaseId = medicalCaseId ?? Guid.NewGuid();

                var consultation = new ConsultationDto
                {
                    MedicalCaseId = _currentMedicalCaseId.Value,
                    PatientId = patient.Id,
                    PatientName = patient.Name ?? string.Empty,
                    StartTime = DateTime.Now,
                    ConsultationStatus = StatusShared.InProgress
                };

                ActiveConsultation = consultation;
                ConsultationStatus = SessionStatus.InProgress;
                StatusMessage?.Invoke(this, new StatusMessageEventArgs
                {
                    Message = "已开始新的诊疗会话",
                    Type = StatusMessageType.Info,
                    Duration = 3000
                });
            }
        }

        public void EndConsultation()
        {
            lock (_gate)
            {
                if (_activeConsultation != null)
                {
                    _activeConsultation.EndTime = DateTime.Now;
                    _activeConsultation.ConsultationStatus = StatusShared.Completed;
                }

                ConsultationStatus = SessionStatus.Completed;
                ActiveConsultation = null;
                _currentMedicalCaseId = null;
            }
        }

        public void SetUserSession(UserDto user, string token)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required", nameof(token));

            CurrentUser = user;
            StatusMessage?.Invoke(this, new StatusMessageEventArgs
            {
                Message = $"欢迎 {user.UserName}",
                Type = StatusMessageType.Success,
                Duration = 3000
            });
        }

        public void ClearUserSession()
        {
            lock (_gate)
            {
                CurrentUser = null;
                CurrentPatient = null;
                ActiveConsultation = null;
                _currentMedicalCaseId = null;
                _consultationStatus = SessionStatus.NotStarted;
            }
        }

        public void Reset()
        {
            ClearUserSession();
        }

        public bool HasActiveSession => _activeConsultation != null;

        public bool IsLoggedIn => _currentUser != null;

        private static SessionStatus MapFromShared(StatusShared status)
        {
            return status switch
            {
                StatusShared.InProgress => SessionStatus.InProgress,
                StatusShared.Completed => SessionStatus.Completed,
                StatusShared.Cancelled => SessionStatus.Cancelled,
                _ => SessionStatus.NotStarted
            };
        }
    }
}
