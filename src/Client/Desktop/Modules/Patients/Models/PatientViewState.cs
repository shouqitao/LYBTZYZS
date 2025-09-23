using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Modules.Patients.Models;

/// <summary>
/// 患者模块视图状态 - 管理UI状态和交互数据
/// 用于PatientManagementViewModel和PatientDetailViewModel
/// 包含筛选条件、排序状态、选中项等UI相关状态
/// </summary>
public partial class PatientViewState : ObservableObject
{
    /// <summary>
    /// 当前选中的患者
    /// </summary>
    [ObservableProperty]
    private PatientItem? selectedPatient;

    /// <summary>
    /// 患者列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PatientItem> patients = new();

    /// <summary>
    /// 搜索关键字
    /// </summary>
    [ObservableProperty]
    private string searchKeyword = string.Empty;

    /// <summary>
    /// 性别筛选
    /// </summary>
    [ObservableProperty]
    private string? genderFilter;

    /// <summary>
    /// 年龄范围筛选 - 最小值
    /// </summary>
    [ObservableProperty]
    private int? ageRangeMin;

    /// <summary>
    /// 年龄范围筛选 - 最大值
    /// </summary>
    [ObservableProperty]
    private int? ageRangeMax;

    /// <summary>
    /// 是否只显示新患者
    /// </summary>
    [ObservableProperty]
    private bool showNewPatientsOnly;

    /// <summary>
    /// 是否只显示有过敏史的患者
    /// </summary>
    [ObservableProperty]
    private bool showAllergicPatientsOnly;

    /// <summary>
    /// 当前页码
    /// </summary>
    [ObservableProperty]
    private int currentPage = 1;

    /// <summary>
    /// 每页显示数量
    /// </summary>
    [ObservableProperty]
    private int pageSize = 20;

    /// <summary>
    /// 总记录数
    /// </summary>
    [ObservableProperty]
    private int totalCount;

    /// <summary>
    /// 排序字段
    /// </summary>
    [ObservableProperty]
    private string sortBy = "Name";

    /// <summary>
    /// 是否降序
    /// </summary>
    [ObservableProperty]
    private bool isDescending;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool isLoading;

    /// <summary>
    /// 是否正在搜索
    /// </summary>
    [ObservableProperty]
    private bool isSearching;

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    [ObservableProperty]
    private bool isEditMode;

    /// <summary>
    /// 是否处于批量选择模式
    /// </summary>
    [ObservableProperty]
    private bool isBatchSelectMode;

    /// <summary>
    /// 批量选中的患者ID列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<int> selectedPatientIds = new();

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string statusMessage = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    [ObservableProperty]
    private string? errorMessage;

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