using System;
using LYBT.Shared.Models.Contracts.Patients;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Patients
{
    /// <summary>
    /// 患者视图模型 - UltraThink架构的协调层
    /// 协调数据、显示、状态和主题四个关注点
    /// 实现了完全的关注点分离
    /// </summary>
    public class PatientViewModel : BindableBase
    {
        #region Fields

        private PatientDisplayViewModel _display;
        private PatientStateViewModel _state;
        private PatientThemeViewModel _theme;

        #endregion

        #region Constructor

        public PatientViewModel(PatientDto patientData)
        {
            if (patientData == null)
                throw new ArgumentNullException(nameof(patientData));

            _display = new PatientDisplayViewModel(patientData);
            _state = new PatientStateViewModel();
            _theme = new PatientThemeViewModel(patientData);
        }

        #endregion

        #region Component ViewModels

        /// <summary>显示逻辑视图模型</summary>
        public PatientDisplayViewModel Display
        {
            get => _display;
            private set => SetProperty(ref _display, value);
        }

        /// <summary>UI状态视图模型</summary>
        public PatientStateViewModel State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        /// <summary>主题样式视图模型</summary>
        public PatientThemeViewModel Theme
        {
            get => _theme;
            private set => SetProperty(ref _theme, value);
        }

        #endregion

        #region Convenient Properties

        /// <summary>患者数据（只读）</summary>
        public PatientDto PatientData => Display.PatientData;

        /// <summary>患者ID</summary>
        public Guid Id => PatientData.Id;

        /// <summary>患者姓名</summary>
        public string Name => PatientData.Name;

        /// <summary>显示名称</summary>
        public string DisplayName => Display.DisplayName;

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get => State.IsSelected;
            set => State.IsSelected = value;
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => State.IsLoading;
            set
            {
                if (value)
                    State.StartLoading();
                else
                    State.StopLoading();
            }
        }

        /// <summary>年龄（计算属性）</summary>
        public int Age
        {
            get
            {
                if (PatientData.BirthDate.HasValue)
                {
                    var today = DateTime.Today;
                    var birthDate = PatientData.BirthDate.Value;
                    int age = today.Year - birthDate.Year;
                    
                    // 如果今年的生日还没到，年龄减一
                    if (birthDate.Date > today.AddYears(-age))
                        age--;
                        
                    return Math.Max(0, age);
                }
                return 0;
            }
        }

        /// <summary>年龄显示文本</summary>
        public string AgeText => PatientData.BirthDate.HasValue ? $"{Age}岁" : "未知";

        /// <summary>出生日期显示文本</summary>
        public string BirthDateText => PatientData.BirthDate?.ToString("yyyy年MM月dd日") ?? "未填写";

        #endregion

        #region Update Methods

        /// <summary>
        /// 更新患者数据
        /// </summary>
        public void UpdatePatientData(PatientDto newPatientData)
        {
            if (newPatientData == null)
                throw new ArgumentNullException(nameof(newPatientData));

            Display.UpdatePatientData(newPatientData);
            Theme.UpdatePatientData(newPatientData);

            // 通知相关属性变化
            RaisePropertyChanged(nameof(PatientData));
            RaisePropertyChanged(nameof(Id));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
        }

        /// <summary>
        /// 开始编辑模式
        /// </summary>
        public void StartEditing()
        {
            State.StartEditing();
        }

        /// <summary>
        /// 结束编辑模式
        /// </summary>
        public void StopEditing()
        {
            State.StopEditing();
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            State.ToggleSelection();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string errorMessage)
        {
            State.SetError(errorMessage);
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            State.ClearError();
        }

        /// <summary>
        /// 重置UI状态
        /// </summary>
        public void ResetState()
        {
            State.Reset();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 创建患者视图模型
        /// </summary>
        public static PatientViewModel Create(PatientDto patientData)
        {
            return new PatientViewModel(patientData);
        }

        /// <summary>
        /// 从现有患者视图模型更新数据
        /// </summary>
        public static PatientViewModel UpdateFrom(PatientViewModel existingViewModel, PatientDto newPatientData)
        {
            existingViewModel.UpdatePatientData(newPatientData);
            return existingViewModel;
        }

        #endregion

        #region Equality and Comparison

        /// <summary>
        /// 判断是否为同一患者
        /// </summary>
        public bool IsSamePatient(PatientViewModel other)
        {
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// 判断是否为同一患者（通过患者数据）
        /// </summary>
        public bool IsSamePatient(PatientDto patientData)
        {
            return patientData != null && Id == patientData.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is PatientViewModel other && IsSamePatient(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}