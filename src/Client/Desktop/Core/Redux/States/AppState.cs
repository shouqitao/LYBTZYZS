using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Immutable;

namespace LYBT.Desktop.Core.Redux.States
{
    /// <summary>
    /// 应用根状态
    /// </summary>
    public record AppState
    {
        public AuthState Auth { get; init; } = AuthState.Initial;
        public PatientState Patients { get; init; } = PatientState.Initial;
        public ConsultationState Consultation { get; init; } = ConsultationState.Initial;
        public UIState UI { get; init; } = UIState.Initial;

        /// <summary>
        /// 创建初始状态
        /// </summary>
        public static AppState Initial => new();
    }

    /// <summary>
    /// 患者状态
    /// </summary>
    public record PatientState
    {
        public ImmutableList<PatientInfo> PatientList { get; init; } = ImmutableList<PatientInfo>.Empty;
        public PatientInfo? CurrentPatient { get; init; }
        public bool IsLoading { get; init; }
        public string? SearchQuery { get; init; }
        public int TotalCount { get; init; }
        public int PageIndex { get; init; }
        public int PageSize { get; init; } = 20;

        public static PatientState Initial => new();
    }

    /// <summary>
    /// 看诊状态
    /// </summary>
    public record ConsultationState
    {
        public Guid? CurrentConsultationId { get; init; }
        public ConsultationStatus Status { get; init; }
        public DiagnosisData? Diagnosis { get; init; }
        public PrescriptionData? Prescription { get; init; }
        public bool IsRecording { get; init; }
        public string? RecordingText { get; init; }

        public static ConsultationState Initial => new();
    }

    /// <summary>
    /// UI状态
    /// </summary>
    public record UIState
    {
        public bool IsGlobalLoading { get; init; }
        public string? GlobalMessage { get; init; }
        public NotificationType? NotificationType { get; init; }
        public string? CurrentRoute { get; init; }
        public ImmutableDictionary<string, bool> DialogStates { get; init; } = ImmutableDictionary<string, bool>.Empty;
        public ImmutableDictionary<string, object> TempData { get; init; } = ImmutableDictionary<string, object>.Empty;

        public static UIState Initial => new();
    }

    #region DTOs

    public record PatientInfo
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Gender { get; init; } = string.Empty;
        public int Age { get; init; }
        public string Phone { get; init; } = string.Empty;
        public string? Address { get; init; }
        public DateTimeOffset LastVisit { get; init; }
    }

    public record DiagnosisData
    {
        public string ChiefComplaint { get; init; } = string.Empty;
        public string PresentIllness { get; init; } = string.Empty;
        public string TongueCoating { get; init; } = string.Empty;
        public string Pulse { get; init; } = string.Empty;
        public string Syndrome { get; init; } = string.Empty;
        public string TreatmentPrinciple { get; init; } = string.Empty;
    }

    public record PrescriptionData
    {
        public Guid Id { get; init; }
        public ImmutableList<HerbItem> Herbs { get; init; } = ImmutableList<HerbItem>.Empty;
        public int Doses { get; init; }
        public string Usage { get; init; } = string.Empty;
        public decimal TotalPrice { get; init; }
    }

    public record HerbItem
    {
        public Guid HerbId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Dosage { get; init; }
        public string Unit { get; init; } = string.Empty;
        public decimal Price { get; init; }
    }

    public enum ConsultationStatus
    {
        NotStarted,
        InProgress,
        Diagnosing,
        Prescribing,
        Completed
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    #endregion

    /// <summary>
    /// 应用根Reducer
    /// </summary>
    public class AppReducer : IReducer<AppState>
    {
        private readonly AuthReducer _authReducer = new();
        private readonly PatientReducer _patientReducer = new();
        private readonly ConsultationReducer _consultationReducer = new();
        private readonly UIReducer _uiReducer = new();

        public AppState Reduce(AppState state, IAction action)
        {
            return new AppState
            {
                Auth = _authReducer.Reduce(state.Auth, action),
                Patients = _patientReducer.Reduce(state.Patients, action),
                Consultation = _consultationReducer.Reduce(state.Consultation, action),
                UI = _uiReducer.Reduce(state.UI, action)
            };
        }
    }

    /// <summary>
    /// 患者Reducer
    /// </summary>
    public class PatientReducer : IReducer<PatientState>
    {
        public PatientState Reduce(PatientState state, IAction action)
        {
            return action switch
            {
                LoadPatientsAction _ => state with { IsLoading = true },
                
                LoadPatientsSuccessAction success => state with
                {
                    IsLoading = false,
                    PatientList = success.Payload.ToImmutableList(),
                    TotalCount = success.Payload.Count
                },

                SelectPatientAction select => state with
                {
                    CurrentPatient = state.PatientList.FirstOrDefault(p => p.Id == select.Payload)
                },

                SearchPatientsAction search => state with
                {
                    SearchQuery = search.Payload,
                    PageIndex = 0
                },

                _ => state
            };
        }
    }

    /// <summary>
    /// 看诊Reducer
    /// </summary>
    public class ConsultationReducer : IReducer<ConsultationState>
    {
        public ConsultationState Reduce(ConsultationState state, IAction action)
        {
            return action switch
            {
                StartConsultationAction start => state with
                {
                    CurrentConsultationId = start.Payload,
                    Status = ConsultationStatus.InProgress
                },

                UpdateDiagnosisAction update => state with
                {
                    Diagnosis = update.Payload,
                    Status = ConsultationStatus.Diagnosing
                },

                SavePrescriptionAction save => state with
                {
                    Prescription = save.Payload,
                    Status = ConsultationStatus.Prescribing
                },

                CompleteConsultationAction _ => state with
                {
                    Status = ConsultationStatus.Completed
                },

                _ => state
            };
        }
    }

    /// <summary>
    /// UI Reducer
    /// </summary>
    public class UIReducer : IReducer<UIState>
    {
        public UIState Reduce(UIState state, IAction action)
        {
            return action switch
            {
                ShowLoadingAction _ => state with { IsGlobalLoading = true },
                HideLoadingAction _ => state with { IsGlobalLoading = false },

                ShowNotificationAction notify => state with
                {
                    GlobalMessage = notify.Payload.Message,
                    NotificationType = notify.Payload.Type
                },

                ClearNotificationAction _ => state with
                {
                    GlobalMessage = null,
                    NotificationType = null
                },

                NavigateAction navigate => state with
                {
                    CurrentRoute = navigate.Payload
                },

                OpenDialogAction open => state with
                {
                    DialogStates = state.DialogStates.SetItem(open.Payload, true)
                },

                CloseDialogAction close => state with
                {
                    DialogStates = state.DialogStates.SetItem(close.Payload, false)
                },

                _ => state
            };
        }
    }

    #region Patient Actions

    public class LoadPatientsAction : ActionBase
    {
        public LoadPatientsAction() : base("PATIENTS/LOAD") { }
    }

    public class LoadPatientsSuccessAction : ActionBase<ImmutableList<PatientInfo>>
    {
        public LoadPatientsSuccessAction(ImmutableList<PatientInfo> patients)
            : base("PATIENTS/LOAD_SUCCESS", patients) { }
    }

    public class SelectPatientAction : ActionBase<Guid>
    {
        public SelectPatientAction(Guid patientId)
            : base("PATIENTS/SELECT", patientId) { }
    }

    public class SearchPatientsAction : ActionBase<string>
    {
        public SearchPatientsAction(string query)
            : base("PATIENTS/SEARCH", query) { }
    }

    #endregion

    #region Consultation Actions

    public class StartConsultationAction : ActionBase<Guid>
    {
        public StartConsultationAction(Guid consultationId)
            : base("CONSULTATION/START", consultationId) { }
    }

    public class UpdateDiagnosisAction : ActionBase<DiagnosisData>
    {
        public UpdateDiagnosisAction(DiagnosisData diagnosis)
            : base("CONSULTATION/UPDATE_DIAGNOSIS", diagnosis) { }
    }

    public class SavePrescriptionAction : ActionBase<PrescriptionData>
    {
        public SavePrescriptionAction(PrescriptionData prescription)
            : base("CONSULTATION/SAVE_PRESCRIPTION", prescription) { }
    }

    public class CompleteConsultationAction : ActionBase
    {
        public CompleteConsultationAction() : base("CONSULTATION/COMPLETE") { }
    }

    #endregion

    #region UI Actions

    public class ShowLoadingAction : ActionBase
    {
        public ShowLoadingAction() : base("UI/SHOW_LOADING") { }
    }

    public class HideLoadingAction : ActionBase
    {
        public HideLoadingAction() : base("UI/HIDE_LOADING") { }
    }

    public class ShowNotificationAction : ActionBase<(string Message, NotificationType Type)>
    {
        public ShowNotificationAction(string message, NotificationType type)
            : base("UI/SHOW_NOTIFICATION", (message, type)) { }
    }

    public class ClearNotificationAction : ActionBase
    {
        public ClearNotificationAction() : base("UI/CLEAR_NOTIFICATION") { }
    }

    public class NavigateAction : ActionBase<string>
    {
        public NavigateAction(string route)
            : base("UI/NAVIGATE", route) { }
    }

    public class OpenDialogAction : ActionBase<string>
    {
        public OpenDialogAction(string dialogName)
            : base("UI/OPEN_DIALOG", dialogName) { }
    }

    public class CloseDialogAction : ActionBase<string>
    {
        public CloseDialogAction(string dialogName)
            : base("UI/CLOSE_DIALOG", dialogName) { }
    }

    #endregion
}