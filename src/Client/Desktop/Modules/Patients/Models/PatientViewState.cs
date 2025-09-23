using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace LYBT.Desktop.Modules.Patients.Models;

/// <summary>
/// 患者模块视图状态 - 管理UI状态和交互数据
/// 用于PatientManagementViewModel和PatientDetailViewModel
/// 包含筛选条件、排序状态、选中项等UI相关状态
/// </summary>
public class PatientViewState : BindableBase
{
    /// <summary>
    /// 当前选中的患者
    /// </summary>
        private PatientItem? _selectedPatient;
    public PatientItem? SelectedPatient
    {
        get => _selectedPatient;
        set => SetProperty(ref _selectedPatient, value);
    }

    /// <summary>
    /// 患者列表
    /// </summary>
    private ObservableCollection<PatientItem> _patients = new();
    public ObservableCollection<PatientItem> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    /// <summary>
    /// 搜索关键字
    /// </summary>
        private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    /// <summary>
    /// 性别筛选
    /// </summary>
        private string? _genderFilter;
    public string? GenderFilter
    {
        get => _genderFilter;
        set => SetProperty(ref _genderFilter, value);
    }

    /// <summary>
    /// 年龄范围筛选 - 最小值
    /// </summary>
        private int? _ageRangeMin;
    public int? AgeRangeMin
    {
        get => _ageRangeMin;
        set => SetProperty(ref _ageRangeMin, value);
    }

    /// <summary>
    /// 年龄范围筛选 - 最大值
    /// </summary>
        private int? _ageRangeMax;
    public int? AgeRangeMax
    {
        get => _ageRangeMax;
        set => SetProperty(ref _ageRangeMax, value);
    }

    /// <summary>
    /// 是否只显示新患者
    /// </summary>
        private bool _showNewPatientsOnly;
    public bool ShowNewPatientsOnly
    {
        get => _showNewPatientsOnly;
        set => SetProperty(ref _showNewPatientsOnly, value);
    }

    /// <summary>
    /// 是否只显示有过敏史的患者
    /// </summary>
        private bool _showAllergicPatientsOnly;
    public bool ShowAllergicPatientsOnly
    {
        get => _showAllergicPatientsOnly;
        set => SetProperty(ref _showAllergicPatientsOnly, value);
    }

    /// <summary>
    /// 当前页码
    /// </summary>
        private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    /// <summary>
    /// 每页显示数量
    /// </summary>
        private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    /// <summary>
    /// 总记录数
    /// </summary>
        private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    /// <summary>
    /// 排序字段
    /// </summary>
        private string _sortBy = "Name";
    public string SortBy
    {
        get => _sortBy;
        set => SetProperty(ref _sortBy, value);
    }

    /// <summary>
    /// 是否降序
    /// </summary>
        private bool _isDescending;
    public bool IsDescending
    {
        get => _isDescending;
        set => SetProperty(ref _isDescending, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
        private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 是否正在搜索
    /// </summary>
        private bool _isSearching;
    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
        private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    /// <summary>
    /// 是否处于批量选择模式
    /// </summary>
        private bool _isBatchSelectMode;
    public bool IsBatchSelectMode
    {
        get => _isBatchSelectMode;
        set => SetProperty(ref _isBatchSelectMode, value);
    }

    /// <summary>
    /// 批量选中的患者ID列表
    /// </summary>
    private ObservableCollection<Guid> _selectedPatientIds = new();
    public ObservableCollection<Guid> SelectedPatientIds
    {
        get => _selectedPatientIds;
        set => SetProperty(ref _selectedPatientIds, value);
    }

    /// <summary>
    /// 状态消息
    /// </summary>
        private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
        private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 是否有数据
    /// </summary>
    public bool HasData => Patients?.Count > 0;

    /// <summary>
    /// 是否为空状态
    /// </summary>
    public bool IsEmpty => !IsLoading && !HasData;

    /// <summary>
    /// 重置筛选条件
    /// </summary>
    public void ResetFilters()
    {
        SearchKeyword = string.Empty;
        GenderFilter = null;
        AgeRangeMin = null;
        AgeRangeMax = null;
        ShowNewPatientsOnly = false;
        ShowAllergicPatientsOnly = false;
        CurrentPage = 1;
        SortBy = "Name";
        IsDescending = false;
    }

    /// <summary>
    /// 清除选择
    /// </summary>
    public void ClearSelection()
    {
        SelectedPatient = null;
        SelectedPatientIds.Clear();
        foreach (var patient in Patients)
        {
            patient.IsSelected = false;
        }
    }

    /// <summary>
    /// 全选
    /// </summary>
    public void SelectAll()
    {
        foreach (var patient in Patients)
        {
            patient.IsSelected = true;
            if (!SelectedPatientIds.Contains(patient.Id))
            {
                SelectedPatientIds.Add(patient.Id);
            }
        }
    }
}
